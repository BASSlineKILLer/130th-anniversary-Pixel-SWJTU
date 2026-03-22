using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using Debug = UnityEngine.Debug;

/// <summary>
/// NPC API 服务：HTTP 请求、JSON 解析、Base64 解码
/// 双层缓存：JSON 响应缓存（秒加载） + 图片 PNG 缓存（避免重复解码）
/// </summary>
public static class NPCApiService
{
    private static string CacheDir => Path.Combine(Application.persistentDataPath, "NPCCache");
    private static string ImageCacheDir => Path.Combine(CacheDir, "Images");
    private static string JsonCachePath => Path.Combine(CacheDir, "api_response.json");

    // 网络层容错参数：应对临时断连、服务端瞬时抖动
    private const int RequestTimeoutSeconds = 20;
    private const int MaxRequestAttempts = 3;
    private const float RetryDelaySeconds = 1.2f;

    // NPC 角色图的 pixelsPerUnit，值越小场景中越大
    // 32x32 像素图 → pixelsPerUnit=32 → 场景中 1 个单位大小
    public const float DefaultPixelsPerUnit = 32f;

    /// <summary>
    /// 获取 NPC 列表。策略：先从本地 JSON 缓存秒加载，再后台请求 API 更新。
    /// onSuccess 可能被调用两次（缓存一次 + 网络一次）。
    /// </summary>
    public static IEnumerator FetchNPCs(string url, Action<List<NPCInfo>> onSuccess, Action<string> onError)
    {
        var totalSw = Stopwatch.StartNew();

        // ===== 阶段 1：本地 JSON 缓存快速加载 =====
        bool cacheLoaded = false;
        if (File.Exists(JsonCachePath))
        {
            var cacheSw = Stopwatch.StartNew();
            try
            {
                string cachedJson = File.ReadAllText(JsonCachePath);
                NPCApiResponse cachedResponse = JsonUtility.FromJson<NPCApiResponse>(cachedJson);
                if (cachedResponse != null && cachedResponse.success && cachedResponse.data != null)
                {
                    var cacheResults = DecodeNPCList(cachedResponse.data);
                    cacheSw.Stop();
                    Debug.Log($"[NPC] 缓存加载: {cacheResults.Count} 个 NPC, 耗时 {cacheSw.ElapsedMilliseconds}ms");
                    onSuccess?.Invoke(cacheResults);
                    cacheLoaded = true;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[NPC] 缓存读取失败，将从网络加载: {e.Message}");
            }
        }

        // ===== 阶段 2：网络请求（带重试） =====
        string json = null;
        string lastError = null;
        long totalHttpMs = 0;

        for (int attempt = 1; attempt <= MaxRequestAttempts; attempt++)
        {
            var httpSw = Stopwatch.StartNew();
            using (UnityWebRequest req = UnityWebRequest.Get(url))
            {
                req.timeout = RequestTimeoutSeconds;
                req.SetRequestHeader("Accept", "application/json");

                yield return req.SendWebRequest();
                httpSw.Stop();
                totalHttpMs += httpSw.ElapsedMilliseconds;

                bool success = req.result == UnityWebRequest.Result.Success;
                string text = req.downloadHandler != null ? req.downloadHandler.text : string.Empty;

                if (success && !string.IsNullOrEmpty(text))
                {
                    json = text;
                    Debug.Log($"[NPC] HTTP 请求成功 (attempt {attempt}/{MaxRequestAttempts}): {httpSw.ElapsedMilliseconds}ms, {text.Length} 字符");
                    break;
                }

                lastError = req.error;
                string bodyInfo = string.IsNullOrEmpty(text) ? "响应体为空" : $"响应体长度={text.Length}";
                Debug.LogWarning($"[NPC] HTTP 请求失败 (attempt {attempt}/{MaxRequestAttempts}, {httpSw.ElapsedMilliseconds}ms): {lastError}, {bodyInfo}");
            }

            if (attempt < MaxRequestAttempts)
                yield return new WaitForSeconds(RetryDelaySeconds * attempt);
        }

        if (string.IsNullOrEmpty(json))
        {
            string msg = $"HTTP 请求失败（重试 {MaxRequestAttempts} 次，总计 {totalHttpMs}ms）: {lastError}";
            if (cacheLoaded)
            {
                Debug.LogWarning($"[NPC] {msg}，已使用本地缓存继续运行");
                yield break;
            }

            Debug.LogWarning($"[NPC] {msg}");
            onError?.Invoke(msg);
            yield break;
        }

        NPCApiResponse response;
        try
        {
            response = JsonUtility.FromJson<NPCApiResponse>(json);
        }
        catch (Exception e)
        {
            string parseMsg = $"JSON 解析失败: {e.Message}";
            if (cacheLoaded)
            {
                Debug.LogWarning($"[NPC] {parseMsg}，已使用本地缓存继续运行");
                yield break;
            }

            onError?.Invoke(parseMsg);
            yield break;
        }

        if (response == null || !response.success || response.data == null)
        {
            const string invalidMsg = "API 返回数据无效或 success=false";
            if (cacheLoaded)
            {
                Debug.LogWarning($"[NPC] {invalidMsg}，已使用本地缓存继续运行");
                yield break;
            }

            onError?.Invoke(invalidMsg);
            yield break;
        }

        // 保存 JSON 缓存供下次秒加载
        SaveJsonCache(json);

        var decodeSw = Stopwatch.StartNew();
        var results = DecodeNPCList(response.data);
        decodeSw.Stop();
        totalSw.Stop();

        Debug.Log($"[NPC] 解码完成: {results.Count} 个, 网络总计 {totalHttpMs}ms, 解码 {decodeSw.ElapsedMilliseconds}ms, 总计 {totalSw.ElapsedMilliseconds}ms");

        onSuccess?.Invoke(results);
    }

    private static List<NPCInfo> DecodeNPCList(NPCRawData[] rawList)
    {
        var results = new List<NPCInfo>();
        foreach (var raw in rawList)
        {
            Sprite sprite = LoadCachedSprite(raw.id);
            if (sprite == null && !string.IsNullOrEmpty(raw.image))
            {
                sprite = Base64ToSprite(raw.image);
                if (sprite != null)
                    SaveSpriteToCache(raw.id, sprite.texture);
            }

            results.Add(new NPCInfo
            {
                Id = raw.id,
                Username = raw.username,
                Message = raw.message,
                Sprite = sprite
            });
        }
        return results;
    }

    public static Sprite Base64ToSprite(string base64DataUrl, float pixelsPerUnit = DefaultPixelsPerUnit)
    {
        if (string.IsNullOrEmpty(base64DataUrl))
            return null;

        try
        {
            string base64 = base64DataUrl;
            int commaIndex = base64.IndexOf(',');
            if (commaIndex >= 0)
                base64 = base64.Substring(commaIndex + 1);

            byte[] bytes = Convert.FromBase64String(base64);
            Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            if (!tex.LoadImage(bytes))
            {
                Debug.LogWarning("[NPC] Base64 图片解码失败");
                return null;
            }

            // pivot (0.5, 0) = 底部居中，NPC 站在点位上而不是飘在空中
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0f), pixelsPerUnit);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[NPC] Base64 解码异常: {e.Message}");
            return null;
        }
    }

    // ===== JSON 缓存 =====

    private static void SaveJsonCache(string json)
    {
        try
        {
            EnsureCacheDir();
            File.WriteAllText(JsonCachePath, json);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[NPC] JSON 缓存保存失败: {e.Message}");
        }
    }

    // ===== 图片缓存 =====

    private static void EnsureCacheDir()
    {
        if (!Directory.Exists(CacheDir))
            Directory.CreateDirectory(CacheDir);
        if (!Directory.Exists(ImageCacheDir))
            Directory.CreateDirectory(ImageCacheDir);
    }

    private static string GetImageCachePath(int npcId)
    {
        return Path.Combine(ImageCacheDir, $"npc_{npcId}.png");
    }

    private static void SaveSpriteToCache(int npcId, Texture2D texture)
    {
        try
        {
            EnsureCacheDir();
            File.WriteAllBytes(GetImageCachePath(npcId), texture.EncodeToPNG());
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[NPC] 图片缓存保存失败 (id={npcId}): {e.Message}");
        }
    }

    private static Sprite LoadCachedSprite(int npcId, float pixelsPerUnit = DefaultPixelsPerUnit)
    {
        string path = GetImageCachePath(npcId);
        if (!File.Exists(path))
            return null;

        try
        {
            byte[] bytes = File.ReadAllBytes(path);
            Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            if (!tex.LoadImage(bytes))
                return null;

            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0f), pixelsPerUnit);
        }
        catch
        {
            return null;
        }
    }

    public static void ClearCache()
    {
        if (Directory.Exists(CacheDir))
        {
            Directory.Delete(CacheDir, true);
            Debug.Log("[NPC] 全部缓存已清除");
        }
    }

    public static long GetCacheSize()
    {
        if (!Directory.Exists(CacheDir))
            return 0;

        long size = 0;
        foreach (var file in Directory.GetFiles(CacheDir, "*", SearchOption.AllDirectories))
            size += new FileInfo(file).Length;
        return size;
    }
}

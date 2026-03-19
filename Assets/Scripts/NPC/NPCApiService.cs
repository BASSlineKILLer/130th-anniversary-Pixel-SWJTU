using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// NPC API 服务：HTTP 请求、JSON 解析、Base64 解码、本地图片缓存
/// </summary>
public static class NPCApiService
{
    private static string CacheDir => Path.Combine(Application.persistentDataPath, "NPCImageCache");

    /// <summary>
    /// 从 API 获取已审核通过的 NPC 列表，解码图片并返回 NPCInfo 列表
    /// </summary>
    public static IEnumerator FetchNPCs(string url, Action<List<NPCInfo>> onSuccess, Action<string> onError)
    {
        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                onError?.Invoke($"HTTP 请求失败: {req.error}");
                yield break;
            }

            NPCApiResponse response;
            try
            {
                response = JsonUtility.FromJson<NPCApiResponse>(req.downloadHandler.text);
            }
            catch (Exception e)
            {
                onError?.Invoke($"JSON 解析失败: {e.Message}");
                yield break;
            }

            if (response == null || !response.success || response.data == null)
            {
                onError?.Invoke("API 返回数据无效或 success=false");
                yield break;
            }

            var results = new List<NPCInfo>();
            foreach (var raw in response.data)
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

            onSuccess?.Invoke(results);
        }
    }

    /// <summary>
    /// 将 base64 data URL 解码为 Sprite
    /// </summary>
    public static Sprite Base64ToSprite(string base64DataUrl)
    {
        if (string.IsNullOrEmpty(base64DataUrl))
            return null;

        try
        {
            // 剥离 data:image/png;base64, 前缀
            string base64 = base64DataUrl;
            int commaIndex = base64.IndexOf(',');
            if (commaIndex >= 0)
                base64 = base64.Substring(commaIndex + 1);

            byte[] bytes = Convert.FromBase64String(base64);
            Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point; // 像素风格
            if (!tex.LoadImage(bytes))
            {
                Debug.LogWarning("[NPCApiService] Base64 图片解码失败");
                return null;
            }

            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[NPCApiService] Base64 解码异常: {e.Message}");
            return null;
        }
    }

    // ===== 本地缓存 =====

    private static void EnsureCacheDir()
    {
        if (!Directory.Exists(CacheDir))
            Directory.CreateDirectory(CacheDir);
    }

    private static string GetCachePath(int npcId)
    {
        return Path.Combine(CacheDir, $"npc_{npcId}.png");
    }

    private static void SaveSpriteToCache(int npcId, Texture2D texture)
    {
        try
        {
            EnsureCacheDir();
            byte[] png = texture.EncodeToPNG();
            File.WriteAllBytes(GetCachePath(npcId), png);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[NPCApiService] 缓存保存失败 (id={npcId}): {e.Message}");
        }
    }

    private static Sprite LoadCachedSprite(int npcId)
    {
        string path = GetCachePath(npcId);
        if (!File.Exists(path))
            return null;

        try
        {
            byte[] bytes = File.ReadAllBytes(path);
            Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            if (!tex.LoadImage(bytes))
                return null;

            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 清除所有 NPC 图片缓存
    /// </summary>
    public static void ClearCache()
    {
        if (Directory.Exists(CacheDir))
        {
            Directory.Delete(CacheDir, true);
            Debug.Log("[NPCApiService] 图片缓存已清除");
        }
    }

    /// <summary>
    /// 获取缓存大小（字节数）
    /// </summary>
    public static long GetCacheSize()
    {
        if (!Directory.Exists(CacheDir))
            return 0;

        long size = 0;
        foreach (var file in Directory.GetFiles(CacheDir))
            size += new FileInfo(file).Length;
        return size;
    }
}

using System;
using UnityEngine;

/// <summary>
/// API 响应包装类，对应 JSON 顶层结构
/// </summary>
[Serializable]
public class NPCApiResponse
{
    public bool success;
    public NPCRawData[] data;
}

/// <summary>
/// API 返回的原始 NPC 数据，字段名与 JSON 一一对应
/// </summary>
[Serializable]
public class NPCRawData
{
    public int id;
    public string username;
    public string message;
    public string config;
    public string image;
    public string status;
    public string created_at;
}

/// <summary>
/// 运行时使用的 NPC 信息，已将 base64 图片解码为 Sprite
/// </summary>
public class NPCInfo
{
    public int Id;
    public string Username;
    public string Message;
    public Sprite Sprite; // 可能为 null（image 为空时）
}

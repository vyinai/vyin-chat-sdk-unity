using System;
using System.Collections.Generic;
using UnityEngine;
using VyinChatSdk;

public static class AndroidJavaUtils
{
    /// <summary>
    /// 將 C# List<string> 轉成 java.util.ArrayList
    /// </summary>
    public static AndroidJavaObject ToJavaArrayList(List<string> list)
    {
        if (list == null || list.Count == 0) return null;

        AndroidJavaObject javaList = new("java.util.ArrayList");
        foreach (var item in list)
        {
            javaList.Call<bool>("add", item);
        }

        return javaList;
    }

    public static VcGroupChannel ToVcGroupChannel(this AndroidJavaObject groupChannel)
    {
        if (groupChannel == null) return null;

        return new VcGroupChannel
        {
            ChannelUrl = groupChannel.Call<string>("getUrl"),
            Name = groupChannel.Call<string>("getName"),
        };
    }

    public static VcBaseMessage ToVcBaseMessage(this AndroidJavaObject baseMessage)
    {
        if (baseMessage == null) return null;

        var sender = baseMessage.Call<AndroidJavaObject>("getSender");
        return new VcBaseMessage
        {
            MessageId = baseMessage.Call<long>("getMessageId"),
            Message = baseMessage.Call<string>("getMessage"),
            ChannelUrl = baseMessage.Call<string>("getChannelUrl"),
            SenderId = sender.Call<string>("getUserId"),
            SenderNickname = sender.Call<string>("getNickname"),
            CreatedAt = baseMessage.Call<long>("getCreatedAt"),
        };
    }

    public static VcUser ToVcUser(this AndroidJavaObject user)
    {
        if (user == null) return null;

        return new VcUser
        {
            UserId = user.Call<string>("getUserId"),
            Nickname = user.Call<string>("getNickname"),
            ProfileUrl = user.Call<string>("getProfileUrl"),
        };
    }

    public static AndroidJavaObject ToAndroidJavaObject(this VcGroupChannelCreateParams param)
    {
        if (param == null) return null;

        AndroidJavaObject javaParams = new("com.gamania.gim.sdk.params.GroupChannelCreateParams");
        javaParams.Call("setName", param.Name);

        AndroidJavaObject javaBooleanValue = new("java.lang.Boolean", param.IsDistinct);
        javaParams.Call("setDistinct", javaBooleanValue);

        var javaOperatorList = ToJavaArrayList(param.OperatorUserIds);
        if (javaOperatorList != null)
        {
            javaParams.Call("setOperatorUserIds", javaOperatorList);
        }

        var javaUserList = ToJavaArrayList(param.UserIds);
        if (javaUserList != null)
        {
            javaParams.Call("setUserIds", javaUserList);
        }

        return javaParams;
    }

    /// <summary>
    /// 將 AndroidJavaObject 的 Exception 轉成可讀錯誤字串
    /// </summary>
    public static string GetErrorMessage(this AndroidJavaObject gimException)
    {
        if (gimException == null) return null;

        try
        {
            int errorCode = gimException.Call<int>("getCode");
            string errorMessage = gimException.Call<string>("getMessage");
            return $"{errorCode}: {errorMessage}";
        }
        catch (System.Exception e)
        {
            return gimException.Call<string>("toString") + " (fallback: " + e.Message + ")";
        }
    }
}
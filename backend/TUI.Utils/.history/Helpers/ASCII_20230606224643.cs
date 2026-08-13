// MIT License
// 开源地址：https://gitee.com/co1024/TUIMvc
// Copyright (c) 2021-2022 1024
// TUI.Mvc=Furion+EF+SqlSugar+Pear layui admin.

namespace TUI.Utils;

public class ASCII
{
    //字符转ASCII码：
    public static int Asc(string character)
    {
        if (character.Length == 1)
        {
            var asciiEncoding = new System.Text.ASCIIEncoding();
            var intAsciiCode = (int)asciiEncoding.GetBytes(character)[0];
            return (intAsciiCode);
        }
        else
        {
            throw new Exception("Character is not valid.");
        }
    }

    public static string Chr(int asciiCode)
    {
        if (asciiCode >= 0 && asciiCode <= 255)
        {
            var asciiEncoding = new System.Text.ASCIIEncoding();
            var byteArray = new byte[] { (byte)asciiCode };
            var strCharacter = asciiEncoding.GetString(byteArray);
            return (strCharacter);
        }
        else
        {
            throw new Exception("ASCII Code is not valid.");
        }
    }
}
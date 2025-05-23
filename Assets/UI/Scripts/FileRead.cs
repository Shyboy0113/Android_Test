using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;
using System.Text.RegularExpressions;
using UnityEditor.Rendering;

/// <summary>
/// CSVȮ���ڷ� �� ���� ���̺� ������ �о string���߸���Ʈ�� ��ȯ�ϴ� Ŭ����
/// </summary>
public static class FileRead
{
	static readonly string SPLIT_RE = @",(?=(?:[^""]*""[^""]*"")*(?![^""]*""))"; //regular expression
	static readonly string LINE_SPLIT_RE = @"\r\n|\n\r|\n|\r";
	static readonly char[] TRIM_CHARS = { ' ' };

    /// <summary>
    /// CSV ������ �о ���ڿ� ���߸���Ʈ�� ��ȯ�մϴ�.
    /// </summary>
    /// <param name="file"> ���� �̸� </param>
    /// <returns> ���ڿ� ���߸���Ʈ�� ��ȯ�� ������ ���� </returns>
	public static List<List<string>> Read(string file, out Dictionary<string, int> columnInfo)
	{
        columnInfo = null;
		TextAsset data = Resources.Load(file) as TextAsset;
        if (data == null)
        {
            Debug.Log("������ �������� �ʽ��ϴ�.");
            return null;
        }

        List<List<string>> rowList = new List<List<string>>();
		string[] lines = Regex.Split(data.text, LINE_SPLIT_RE);

        if (lines.Length <= 1)
        {
            Debug.Log("���� ������ �������� �ʽ��ϴ�.");
            return null;
        }

        // ù ���� Column Info
        columnInfo = new Dictionary<string, int>();
        string[] columns = Regex.Split(lines[0], SPLIT_RE);
        for (int i = 0; i < columns.Length; i++)
        {
            if (!columnInfo.TryAdd(columns[i], i))
                Debug.LogError($"ReadError: {file} has two columns at the same name");
        }

		// �ι�° ���� �����͸� ��Ƽ�, ��ȯ
        for (int i = 1; i < lines.Length; i++)
        {
            string[] values = Regex.Split(lines[i], SPLIT_RE);
            if (values.Length == 0 || values[0] == "") continue;

            List<string> columnList = new List<string>();
            for (int j = 0; j < values.Length; j++)
            {
                string value = values[j];
                //���� ����
                value = value.TrimStart(TRIM_CHARS).TrimEnd(TRIM_CHARS);
                string finalvalue = value;
                columnList.Add(finalvalue);
            }
            rowList.Add(columnList);
        }
        Resources.UnloadAsset(data);
        return rowList;
	}

    public static T[] ConvertStringToArray<T>(string input)
    {
	    if (input == "") return Array.Empty<T>();
	    
	    T type = default;
	    if ((type is int or float) is false)
	    {
		    Debug.LogError("Cant String to array with type" + typeof(T));
		    return null;
	    }
	    
	    // ���ڿ����� "�� �����ϰ�, ��ǥ�� �и��Ͽ� ���� �κи� �����ɴϴ�.
	    string[] numbersAsString = input.Trim('\"', '\"').Split(',');

	    // ����� ��ȯ�� �迭�� �ʱ�ȭ�մϴ�.
	    T[] result = new T[numbersAsString.Length];
	    if(numbersAsString.Length == 0) return result;

	    // ��ȯ�Ͽ� ��� �迭�� �����մϴ�.
	    for (int i = 0; i < numbersAsString.Length; i++)
	    {
		    if (result is int[] ints)
		    {
			    ints[i] = int.Parse(numbersAsString[i]);
		    }

		    if (result is float[] floats)
		    {
			    floats[i] = float.Parse(numbersAsString[i]);
		    }
	    }

	    return result;
    }
}

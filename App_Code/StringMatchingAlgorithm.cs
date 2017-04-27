using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;

/// <summary>
/// Summary description for Class1
/// </summary>
public class StringMatchingAlgorithm
{
    public static int[] computeFail(string pattern)
    {
        int[] fail = new int[pattern.Length];
        fail[0] = 0;

        int m = pattern.Length;
        int j = 0;
        int i = 1;
        while (i < m)
        {
            if (pattern[j] == pattern[i])
            {
                fail[i] = j + 1;
                i++;
                j++;
            }
            else if (j > 0) { j = fail[j - 1]; }
            else { fail[i] = 0; i++; }
        }
        return fail;
    }

    public static int KMPMatch(string text, string pattern)
    {
        int n = text.Length;
        int m = pattern.Length;
        int[] fail = computeFail(pattern);

        int i = 0, j = 0;

        while (i < n)
        {
            if (pattern[j] == text[i])
            {
                if (j == m - 1) { return i - m + 1; }
                i++;
                j++;
            }
            else if (j > 0) { j = fail[j - 1]; }
            else { i++; }
        }
        return -1;
    }

    public static int[] buildLast(string pattern)
    {
        int[] last = new int[256];
        for (int i = 0; i < 256; i++) { last[i] = -1; }
        for (int i = 0; i < pattern.Length; i++) { last[pattern[i]] = i; }
        return last;
    }

    public static int BMMatch(string text, string pattern)
    {
        int[] last = buildLast(pattern);
        int n = text.Length;
        int m = pattern.Length;
        int skip;
        for (int i = 0; i <= n-m; i += skip)
        {
            skip = 0;
            for (int j = m-1; j >= 0; j--)
            {
                if (pattern[j] != text[i+j])
                {
                    skip = Math.Max(1, j - last[text[i + j]]);
                    break;
                }
            }
            if (skip == 0) return i;
        }
        return n;
        /*int i = m - 1;

        if (i > n - 1) { return -1; }

        int j = m - 1;
        do
        {
            if (pattern[j] == text[i])
            {
                if (j == 0) { return i; }
                else { i--; j--; }
            }
            else
            {
                int lo = last[text[i]];
                i = i + m - Math.Min(j, 1 + lo);
                j = m - 1;
            }
        } while (i <= n - 1);

        return -1;*/
    }

    public static void regexMatch(string text, string expr)
    {
        Console.WriteLine("Expression : {0}", expr);
        MatchCollection mc = Regex.Matches(text, expr);
        foreach (Match m in mc)
        {
            Console.WriteLine(m);
        }
    }

    public static void Main(string[] args)
    {
        for (;;)
        {
            string text = Console.ReadLine();
            string pattern = Console.ReadLine();

            Console.WriteLine("Text : {0}", text);
            Console.WriteLine("Pattern : {0}", pattern);

            // regexMatch(text, pattern);
             int posn = BMMatch(text, pattern);
            if (posn == -1)
            {
                Console.WriteLine("Pattern not found");
            }
            else
            {
                Console.WriteLine("Pattern starts at position {0}", posn);
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;

namespace black_wing
{
    static class ReadCSV
    {
        private static List<uint> inKey1List = new List<uint>();
        private static List<uint> inKey2List = new List<uint>();
        private static List<uint> outKeyList = new List<uint>();
        private static List<string> outStringList = new List<string>();
        private static uint[] inKey1;
        private static uint[] inKey1Dis;
        private static uint[] inKey2;
        private static uint[] outKey;
        private static string[] outString;
        private static uint stringUint = 1000;

        public static void CsvToArray()
        {
            try
            {
                // csvファイルを開く
                using (var sr = new System.IO.StreamReader(@"KeyList.csv", System.Text.Encoding.GetEncoding("shift_jis")))
                {
                    //一行目は項目なので読み飛ばす
                    sr.ReadLine();
                    // ストリームの末尾まで繰り返す
                    while (!sr.EndOfStream)
                    {
                        // ファイルから一行読み込む
                        var line = sr.ReadLine();
                        // 読み込んだ一行をカンマ毎に分けて配列に格納する
                        var values = line.Split(',');
                        inKey1List.Add(Convert.ToUInt32(values[1], 16));
                        inKey2List.Add(Convert.ToUInt32(values[3], 16));
                        if (values[5].Substring(0, 2) == "0x")
                        {
                            outKeyList.Add(Convert.ToUInt32(values[5], 16));
                        }
                        else
                        {
                            outKeyList.Add(stringUint);
                            outStringList.Add(values[5]);
                            stringUint += 1;
                        }
                    }
                }

                inKey1 = inKey1List.ToArray();
                inKey2 = inKey2List.ToArray();
                outKey = outKeyList.ToArray();
                IEnumerable<uint> inKey1ListDis = inKey1List.Distinct();
                inKey1Dis = inKey1ListDis.ToArray();
                outString = outStringList.ToArray();
            }
            catch (System.Exception e)
            {
                // ファイルを開くのに失敗したとき
                System.Console.WriteLine(e.Message);
            }
        }

        public static uint[] getInKey1()
        {
            return inKey1;
        }

        public static uint[] getInKey2()
        {
            return inKey2;
        }

        public static uint[] getOutKey()
        {
            return outKey;
        }

        public static uint[] getInKey1Dis()
        {
            return inKey1Dis;
        }

        public static string[] getOutString()
        {
            return outString;
        }


    }
}

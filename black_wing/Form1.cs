using System;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace black_wing
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        // マウスを操作するためのデータ構造
        [StructLayout(LayoutKind.Sequential)]
        private struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public int mouseData;
            public int dwFlags;
            public int time;
            public int dwExtraInfo;
        };

        // キーボードを操作するためのデータ構造
        [StructLayout(LayoutKind.Sequential)]
        private struct KEYBDINPUT
        {
            public short wVk;
            public short wScan;
            public int dwFlags;
            public int time;
            public int dwExtraInfo;
        };

        // ハードウェアを操作するためのデータ構造
        [StructLayout(LayoutKind.Sequential)]
        private struct HARDWAREINPUT
        {
            public int uMsg;
            public short wParamL;
            public short wParamH;
        }

        // SendInputで動作を行うための引数(下記の構造固定)
        [StructLayout(LayoutKind.Explicit)]
        private struct INPUT
        {
            [FieldOffset(0)] public int type;
            [FieldOffset(4)] public MOUSEINPUT mi;
            [FieldOffset(4)] public KEYBDINPUT ki;
            [FieldOffset(4)] public HARDWAREINPUT hi;
        };

        // 指定したキー入力などを行う関数
        [DllImport("user32.dll")]
        private extern static void SendInput(
            int nInputs, ref INPUT pInputs, int cbsize);

        // 仮想キーコードをスキャンコードに変換
        [DllImport("user32.dll", EntryPoint = "MapVirtualKeyA")]
        private extern static int MapVirtualKey(
            int wCode, int wMapType);

        private const int INPUT_MOUSE = 0;                  // マウスイベント(SendInputにtype引数で使用)
        private const int INPUT_KEYBOARD = 1;               // キーボードイベント(SendInputにtype引数で使用)
        private const int INPUT_HARDWARE = 2;               // ハードウェアイベント(SendInputにtype引数で使用)


        private const int KEYEVENTF_KEYDOWN = 0x0;          // キーを押す
        private const int KEYEVENTF_KEYUP = 0x2;            // キーを離す
        private const int KEYEVENTF_EXTENDEDKEY = 0x1;      // 拡張コード
        private const int VK_SHIFT = 0x10;                  // SHIFTキー


        //フラグ管理のための変数
        private uint Key1 = 0;

         [STAThread]
        void hookKeyboardTest(ref KeyboardHook.StateKeyboard s)
        {
            uint InputKey = (uint)s.Key;

            //無変換をフラグ管理する
            if (Array.IndexOf(ReadCSV.getInKey1Dis(), InputKey) != -1 && s.Stroke == KeyboardHook.Stroke.KEY_DOWN)
            {
                Key1 = InputKey;
            }

            if (InputKey == Key1 && s.Stroke == KeyboardHook.Stroke.KEY_UP)
            {
                Key1 = 0;
            }

            int KeyNumber = Array.IndexOf(ReadCSV.getInKey2(), InputKey);
            while (0 <= KeyNumber && Key1 != ReadCSV.getInKey1()[KeyNumber])
            {
                if (KeyNumber + 1 < ReadCSV.getInKey2().Length)
                {
                    //次の要素を検索する
                    KeyNumber = Array.IndexOf(ReadCSV.getInKey2(), InputKey, KeyNumber + 1);
                }
                else
                {
                    //最後まで検索したときはループを抜ける
                    break;
                }
            }

            if(KeyNumber != -1 && ReadCSV.getOutKey()[KeyNumber] >= 1000)
            {
                string CBstring = ReadCSV.getOutString()[ReadCSV.getOutKey()[KeyNumber] - 1000];
                Clipboard.SetText(CBstring);
            }

            //無変換＋Ｆ＝BackSpace
            if (KeyNumber != -1 && s.Stroke == KeyboardHook.Stroke.KEY_DOWN)
            {
                // キーボード操作実行用のデータ
                const int num = 2;
                INPUT[] inp = new INPUT[num];
                short OutKey = (short)ReadCSV.getOutKey()[KeyNumber];

                // (0)キーボードを押す
                inp[0].type = INPUT_KEYBOARD;
                inp[0].ki.wVk = OutKey;
                inp[0].ki.wScan = (short)MapVirtualKey(inp[0].ki.wVk, 0);
                inp[0].ki.dwFlags = KEYEVENTF_EXTENDEDKEY | KEYEVENTF_KEYDOWN;
                inp[0].ki.dwExtraInfo = 0;
                inp[0].ki.time = 0;

                // (1)キーボードを離す
                inp[1].type = INPUT_KEYBOARD;
                inp[1].ki.wVk = OutKey;
                inp[1].ki.wScan = (short)MapVirtualKey(inp[1].ki.wVk, 0);
                inp[1].ki.dwFlags = KEYEVENTF_EXTENDEDKEY | KEYEVENTF_KEYUP;
                inp[1].ki.dwExtraInfo = 0;
                inp[1].ki.time = 0;

                // キーボード操作実行
                KeyboardHook.Pause(); //出力するキーイベントがフックされないため、一時停止
                SendInput(num, ref inp[0], Marshal.SizeOf(inp[0]));　//実際にキーを出力
                KeyboardHook.ReStart(); //一時停止の解除
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            ReadCSV.CsvToArray();
            KeyboardHook.AddEvent(hookKeyboardTest); // hookKeyboardTestをイベントに追加
            KeyboardHook.Start();
        }

        private void Form1_Activated(object sender, EventArgs e)
        {
            this.Hide();
        }
    }
}

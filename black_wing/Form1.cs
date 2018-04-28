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
        byte Flag2;
        Keys[] SearchKeys = { Keys.F, Keys.H, Keys.J, Keys.K, Keys.L, Keys.M, Keys.G, Keys.Oemplus, Keys.IMEConvert };
        Keys[] OutKeys = { Keys.Back, Keys.Left, Keys.Down, Keys.Up, Keys.Right, Keys.Enter, Keys.Delete, Keys.KanjiMode, Keys.Escape };

        void hookKeyboardTest(ref KeyboardHook.StateKeyboard s)
        {
            short OutKey = 0;

            Keys w = s.Key;
            uint sf = s.ScanCode;

            //無変換をフラグ管理する
            if (s.Key == Keys.IMENonconvert && s.Stroke == KeyboardHook.Stroke.KEY_DOWN)
            {
                Flag2 = 1;
            }

            if (s.Key == Keys.IMENonconvert && s.Stroke == KeyboardHook.Stroke.KEY_UP)
            {
                Flag2 = 0;
            }

            int KeyNu = Array.IndexOf(SearchKeys, s.Key);

            if (KeyNu != -1)
            {
                OutKey = (short)OutKeys[KeyNu];
            }

            //無変換＋Ｆ＝BackSpace
            if (OutKey != 0 && s.Stroke == KeyboardHook.Stroke.KEY_DOWN && Flag2 == 1)
            {
                // キーボード操作実行用のデータ
                const int num = 2;
                INPUT[] inp = new INPUT[num];

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
            KeyboardHook.AddEvent(hookKeyboardTest); // hookKeyboardTestをイベントに追加
            KeyboardHook.Start();
        }

        private void Form1_Activated(object sender, EventArgs e)
        {
            this.Hide();
        }
    }
}

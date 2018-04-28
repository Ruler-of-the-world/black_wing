using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace black_wing
{
    class KeyboardHook
    {
        private static class NativeMethods
        {
            /// <summary>
            /// フックプロシージャのデリゲート
            /// </summary>
            /// <param name="nCode">フックプロシージャに渡すフックコード</param>
            /// <param name="msg">フックプロシージャに渡す値</param>
            /// <param name="msllhookstruct">フックプロシージャに渡す値</param>
            /// <returns>フックチェーン内の次のフックプロシージャの戻り値</returns>
            public delegate System.IntPtr KeyboardHookCallback(int nCode, uint msg, ref KBDLLHOOKSTRUCT kbdllhookstruct);

            /// <summary>
            /// アプリケーション定義のフックプロシージャをフックチェーン内にインストールします。
            /// フックプロシージャをインストールすると、特定のイベントタイプを監視できます。
            /// 監視の対象になるイベントは、特定のスレッド、または呼び出し側スレッドと同じデスクトップ内のすべてのスレッドに関連付けられているものです。
            /// </summary>
            /// <param name="idHook">フックタイプ</param>
            /// <param name="lpfn">フックプロシージャ</param>
            /// <param name="hMod">アプリケーションインスタンスのハンドル</param>
            /// <param name="dwThreadId">スレッドの識別子</param>
            /// <returns>関数が成功すると、フックプロシージャのハンドルが返ります。関数が失敗すると、NULL が返ります。</returns>
            [System.Runtime.InteropServices.DllImport("user32.dll")]
            public static extern System.IntPtr SetWindowsHookEx(int idHook, KeyboardHookCallback lpfn, System.IntPtr hMod, uint dwThreadId);

            /// <summary>
            /// 現在のフックチェーン内の次のフックプロシージャに、フック情報を渡します。
            /// フックプロシージャは、フック情報を処理する前でも、フック情報を処理した後でも、この関数を呼び出せます。
            /// </summary>
            /// <param name="hhk">現在のフックのハンドル</param>
            /// <param name="nCode">フックプロシージャに渡すフックコード</param>
            /// <param name="msg">フックプロシージャに渡す値</param>
            /// <param name="msllhookstruct">フックプロシージャに渡す値</param>
            /// <returns>フックチェーン内の次のフックプロシージャの戻り値</returns>
            [System.Runtime.InteropServices.DllImport("user32.dll")]
            public static extern System.IntPtr CallNextHookEx(System.IntPtr hhk, int nCode, uint msg, ref KBDLLHOOKSTRUCT kbdllhookstruct);

            /// <summary>
            /// SetWindowsHookEx 関数を使ってフックチェーン内にインストールされたフックプロシージャを削除します。
            /// </summary>
            /// <param name="hhk">削除対象のフックプロシージャのハンドル</param>
            /// <returns>関数が成功すると、0 以外の値が返ります。関数が失敗すると、0 が返ります。</returns>
            [System.Runtime.InteropServices.DllImport("user32.dll")]
            [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
            public static extern bool UnhookWindowsHookEx(System.IntPtr hhk);
        }

        // キーボードの状態の構造体
        // 構造体とは(struct)? 簡易クラスのように使用する。クラスは参照型であるが構造体は値型である
        // 16バイトまでは構造体の方が早く便利だがメモリ的にそれ以上はクラスの方が良い
        // 後、インターフェースなどは実装できるが継承はできない
        // コンストラクタ(オブジェクト生成時の処理)で引数を使うことができない
        // クラスと違い生成したオブジェクトをコピーしても参照型にならない　これは便利！
        public struct StateKeyboard
        {
            public Stroke Stroke;
            public System.Windows.Forms.Keys Key;
            public uint ScanCode;
            public uint Flags;
            public uint Time;
            public System.IntPtr ExtraInfo;
        }

        // 挙動の列挙型 列挙型とは(enum)？　定数を定数名のまま使う感じ
        public enum Stroke
        {
            KEY_DOWN,
            KEY_UP,
            SYSKEY_DOWN,
            SYSKEY_UP,
            UNKNOWN
        }


        // キーボードのグローバルフックを実行しているかどうかを取得、設定します。(プロパティ)
        // プロパティとは変数のように使えるがprivateとpublicを設定できるので参照の制限ができる (下記は値を設定するsetのみクラス内で設定できる)
        public static bool IsHooking
        {
            get;
            private set;
        }

        // キーボードのグローバルフックを一時停止しているかどうかを取得、設定します。(プロパティ)
        public static bool IsPaused
        {
            get;
            private set;
        }

        // キーボードの状態を取得、設定します。(StateKeyboardの構造？型？を持ったフィールド)
        public static StateKeyboard State;

        /// <summary>
        /// フックプロシージャ内でのイベント用のデリゲート
        /// </summary>
        /// <param name="msg">キーボードに関するウィンドウメッセージ</param>
        /// <param name="msllhookstruct">低レベルのキーボードの入力イベントの構造体</param>
        public delegate void HookHandler(ref StateKeyboard state);

        /// <summary>
        /// 低レベルのキーボードの入力イベントの構造体
        /// </summary>
        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        private struct KBDLLHOOKSTRUCT
        {
            public uint vkCode;
            public uint scanCode;
            public uint flags;
            public uint time;
            public System.IntPtr dwExtraInfo;
        }

        /// <summary>
        /// フックプロシージャのハンドル
        /// </summary>
        private static System.IntPtr Handle;

        // 無変換のフラグ
        public static byte Flag;

        // 登録イベントのリストを取得、設定します。
        private static System.Collections.Generic.List<HookHandler> Events;

        /// <summary>
        /// フックプロシージャ内でのイベント
        /// </summary>
        private static event HookHandler HookEvent;

        /// <summary>
        /// フックチェーンにインストールするフックプロシージャのイベント
        /// </summary>
        private static event NativeMethods.KeyboardHookCallback hookCallback;

        /// <summary>
        /// フックプロシージャをフックチェーン内にインストールし、キーボードのグローバルフックを開始します。
        /// </summary>
        /// <exception cref="System.ComponentModel.Win32Exception"></exception>
        public static void Start()
        {
            IsHooking = true;
            IsPaused = false;

            hookCallback = HookProcedure;
            System.IntPtr h = System.Runtime.InteropServices.Marshal.GetHINSTANCE(typeof(KeyboardHook).Assembly.GetModules()[0]);

            // WH_KEYBOARD_LL = 13;
            Handle = NativeMethods.SetWindowsHookEx(13, hookCallback, h, 0);

            if (Handle == System.IntPtr.Zero)
            {
                IsHooking = false;
                IsPaused = true;

                throw new System.ComponentModel.Win32Exception();
            }
        }

        // キーボードのグローバルフックを一時停止します
        public static void Pause()
        {
            IsPaused = true;
        }

        // キーボードのグローバルフックを一時停止を解除します
        public static void ReStart()
        {
            IsPaused = false;
        }


        // キーボード操作時のイベントを追加する
        public static void AddEvent(HookHandler hookHandler)
        {
            if (Events == null)
            {
                Events = new System.Collections.Generic.List<HookHandler>();
            }

            Events.Add(hookHandler);
            HookEvent += hookHandler;
        }

        /// フックチェーンにインストールするフックプロシージャ
        /// </summary>
        /// <param name="nCode">フックプロシージャに渡すフックコード</param>
        /// <param name="msg">フックプロシージャに渡す値</param>
        /// <param name="msllhookstruct">フックプロシージャに渡す値</param>
        /// <returns>フックチェーン内の次のフックプロシージャの戻り値</returns>
        private static System.IntPtr HookProcedure(int nCode, uint msg, ref KBDLLHOOKSTRUCT s)
        {

            if (nCode >= 0 && HookEvent != null && !IsPaused)
            {

                State.Stroke = GetStroke(msg);
                State.Key = (System.Windows.Forms.Keys)s.vkCode;
                State.ScanCode = s.scanCode;
                State.Flags = s.flags;
                State.Time = s.time;
                State.ExtraInfo = s.dwExtraInfo;

                //登録してあるイベントを発生させます
                HookEvent(ref State);

                if (s.vkCode == 0x1D && GetStroke(msg) == Stroke.KEY_DOWN)
                {
                    Flag = 1;
                    return (System.IntPtr)1;
                }

                if (s.vkCode == 0x1D && GetStroke(msg) == Stroke.KEY_UP)
                {
                    Flag = 0;
                }

                uint[] KeysN = { 0x46, 0x48, 0x4A, 0x4B, 0x4C, 0x4D, 0x47, 0xBB, 0x1C };
                byte KeyInNu = 0;
                uint w = s.vkCode;

                for (int i = 0; i < KeysN.Length; i++)
                {
                    if (s.vkCode == KeysN[i])
                    {
                        KeyInNu = 1;
                    }
                }

                if (KeyInNu == 1 && Flag == 1)
                {
                    return (System.IntPtr)1;
                }

                if (s.vkCode == 0x1C || s.vkCode == 0xF2)
                {
                    return (System.IntPtr)1;
                }

            }

            return NativeMethods.CallNextHookEx(Handle, nCode, msg, ref s);
        }

        // キーの状態を決定する
        private static Stroke GetStroke(uint msg)
        {
            switch (msg)
            {
                case 0x100:
                    return Stroke.KEY_DOWN;

                case 0x101:
                    return Stroke.KEY_UP;

                case 0x104:
                    return Stroke.SYSKEY_DOWN;

                case 0x105:
                    return Stroke.SYSKEY_UP;

                default:
                    return Stroke.UNKNOWN;
            }
        }

    }
}

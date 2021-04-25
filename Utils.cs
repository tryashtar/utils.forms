using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TryashtarUtils.Forms
{
    public static class Utils
    {
        // hack copied from https://stackoverflow.com/a/580264
        // set text longer than 64 characters through reflection (still limited to 127)
        public static void SetNotifyIconText(NotifyIcon ni, string text)
        {
            if (text.Length >= 128) throw new ArgumentOutOfRangeException("Text limited to 127 characters");
            Type t = typeof(NotifyIcon);
            BindingFlags hidden = BindingFlags.NonPublic | BindingFlags.Instance;
            t.GetField("text", hidden).SetValue(ni, text);
            if ((bool)t.GetField("added", hidden).GetValue(ni))
                t.GetMethod("UpdateIcon", hidden).Invoke(ni, new object[] { true });
        }

        // list of file extensions to a filter string for OpenFileDialog
        public static string ToFilter(IEnumerable<string> extensions)
        {
            return String.Join("; ", extensions.Select(x => "*" + x));
        }

        // get flattened list of all controls on a parent container
        public static IEnumerable<Control> GetAllControls(Control root)
        {
            var queue = new Queue<Control>();
            queue.Enqueue(root);
            do
            {
                var control = queue.Dequeue();
                yield return control;
                foreach (var child in control.Controls.OfType<Control>())
                {
                    queue.Enqueue(child);
                }
            } while (queue.Count > 0);
        }

        // for disabling sleep
        [Flags]
        private enum EXECUTION_STATE : uint
        {
            ES_AWAYMODE_REQUIRED = 0x00000040,
            ES_CONTINUOUS = 0x80000000,
            ES_DISPLAY_REQUIRED = 0x00000002,
            ES_SYSTEM_REQUIRED = 0x00000001
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern EXECUTION_STATE SetThreadExecutionState(EXECUTION_STATE esFlags);
        private static EXECUTION_STATE Current = 0;

        public static void DisableSleep()
        {
            var disable = EXECUTION_STATE.ES_CONTINUOUS | EXECUTION_STATE.ES_DISPLAY_REQUIRED | EXECUTION_STATE.ES_SYSTEM_REQUIRED | EXECUTION_STATE.ES_AWAYMODE_REQUIRED;
            if (Current != disable)
            {
                SetThreadExecutionState(disable);
                Current = disable;
            }
        }

        public static void EnableSleep()
        {
            var enable = EXECUTION_STATE.ES_CONTINUOUS;
            if (Current != enable)
            {
                SetThreadExecutionState(enable);
                Current = enable;
            }
        }

        // helpers for placing controls
        public static void PlaceAfterLeft(this Control control, Control after, int distance)
        {
            control.Left = after.Left + distance;
        }

        public static void PlaceBeforeLeft(this Control control, Control after, int distance)
        {
            control.Left = after.Left - control.Width - distance;
        }

        public static void PlaceAfterRight(this Control control, Control after, int distance)
        {
            control.Left = after.Right + distance;
        }

        public static void PlaceBeforeRight(this Control control, Control after, int distance)
        {
            control.Left = after.Right - control.Width - distance;
        }

        public static void PlaceAboveBottom(this Control control, Control after, int distance)
        {
            control.Top = after.Bottom - control.Height - distance;
        }

        public static void PlaceAboveTop(this Control control, Control after, int distance)
        {
            control.Top = after.Top - control.Height - distance;
        }

        public static void PlaceBelowTop(this Control control, Control after, int distance)
        {
            control.Top = after.Top + distance;
        }

        public static int GetCenter(this Control control)
        {
            return control.Top + (control.Height / 2);
        }

        public static void SetCenter(this Control control, int center)
        {
            control.Top = center - (control.Height / 2);
        }

        public static bool IsEditingText(Control ctrl)
        {
            while (true)
            {
                if (ctrl is TextBox)
                    return true;
                else if (ctrl is ContainerControl container)
                    ctrl = container.ActiveControl;
                else
                    return false;
            }
        }

        public static DataObject Merge(DataObject obj1, DataObject obj2)
        {
            var result = new DataObject();

            // naive transfer
            // gets (hopefully) everything, but replaces rather than combining
            var formats1 = obj1.GetFormats();
            var formats2 = obj2.GetFormats();
            foreach (var format in formats1)
            {
                var data = obj1.GetData(format);
                result.SetData(format, data);
            }
            foreach (var format in formats2)
            {
                var data = obj2.GetData(format);
                result.SetData(format, data);
            }

            // specific mergeable transfers
            var text1 = obj1.GetText();
            var text2 = obj2.GetText();
            var text_result = Merge(text1, text2);
            if (text_result != null)
                result.SetText(text_result);

            var file1 = obj1.GetFileDropList();
            var file2 = obj2.GetFileDropList();
            var file_result = Merge(file1, file2);
            if (file_result != null)
                result.SetFileDropList(file_result);

            return result;
        }

        private static string Merge(string str1, string str2)
        {
            if (str1 == null)
                return str2;
            if (str2 == null)
                return str1;
            return str1 + Environment.NewLine + str2;
        }

        private static StringCollection Merge(StringCollection col1, StringCollection col2)
        {
            if (col1 == null)
                return col2;
            if (col2 == null)
                return col1;
            var result = new StringCollection();
            result.AddRange(col1.Cast<string>().ToArray());
            result.AddRange(col2.Cast<string>().ToArray());
            return result;
        }

        public static string ExceptionMessage(Exception exception)
        {
            string message = exception.Message;
            if (exception is AggregateException aggregate)
                message += Environment.NewLine + String.Join(Environment.NewLine, aggregate.InnerExceptions.Select(ExceptionMessage));
            else
            {
                if (exception is WebException web && web.Response != null)
                {
                    using (var reader = new StreamReader(web.Response.GetResponseStream()))
                    {
                        message += Environment.NewLine + reader.ReadToEnd();
                    }
                }
                if (exception.InnerException != null)
                    message += Environment.NewLine + ExceptionMessage(exception.InnerException);
            }
            return message;
        }
    }
}

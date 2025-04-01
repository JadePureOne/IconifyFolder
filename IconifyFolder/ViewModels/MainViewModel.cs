using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IconifyFolder.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace IconifyFolder.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _selectedFolder;
        
        [ObservableProperty]
        private string _targetFolder;
        
        [ObservableProperty]
        private bool _autoScanSubfolders;

      

        public ObservableCollection<ProgramItem> Programs { get; } = new ObservableCollection<ProgramItem>();

        #region BrowseFolder&GetIcons

        [RelayCommand]
        public void BrowseFolder()
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "请选择文件夹";
                dialog.ShowNewFolderButton = false;

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    SelectedFolder = dialog.SelectedPath;
                    LoadPrograms();
                }
            }
        }

        private void LoadPrograms()
        {
            Programs.Clear();

            if (string.IsNullOrEmpty(SelectedFolder))
                return;

            var searchOption = AutoScanSubfolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            string folderName = new DirectoryInfo(SelectedFolder).Name;

            foreach (var file in Directory.GetFiles(SelectedFolder, "*.exe", searchOption))
            {
                var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(file);
                var icon = GetIcon(file);

                // 检查文件名是否包含文件夹名的一部分
                bool isCloselyMatching = fileNameWithoutExtension.Contains(folderName, StringComparison.OrdinalIgnoreCase);
                if (isCloselyMatching)
                {
                    Programs.Add(new ProgramItem
                    {
                        Name = Path.GetFileName(file),
                        FilePath = file,
                        Icon = icon
                    });
                    break;
                }

            }
        }

        private Icon GetIcon(string filePath)
        {
            // 实现从可执行文件中提取图标的逻辑
            return Icon.ExtractAssociatedIcon(filePath);
        }
        #endregion


        #region ApplyIcons
        [RelayCommand]
        public void ApplyIcons()
        {
            foreach (var program in Programs)
            {
                if (program.IsSelected)
                {
                    try
                    {
                        string folderPath = Path.GetDirectoryName(program.FilePath);
                        Icon icon = program.Icon;

                        if (icon != null && Directory.Exists(folderPath))
                        {
                            string tempIconPath = Path.Combine(folderPath, $"{program.Name}_icon.ico");
                            SaveIconToFile(icon, tempIconPath);
                            ApplyIconToFolder(folderPath, tempIconPath);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error applying icon to {program.Name}: {ex.Message}");
                    }
                }
            }
        }

        private void SaveIconToFile(Icon icon, string iconPath)
        {
            using (FileStream fs = new FileStream(iconPath, FileMode.Create))
            {
                icon.Save(fs);
            }
        }

        // 正确的Win32 API声明
        [DllImport("shell32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern void SHChangeNotify(uint wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);

        [DllImport("shell32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern void SHChangeNotify(uint wEventId, uint uFlags, string dwItem1, IntPtr dwItem2);

        // 定义正确的系统常量
        private const uint SHCNE_ASSOCCHANGED = 0x08000000;
        private const uint SHCNE_UPDATEITEM = 0x00002000;
        private const uint SHCNE_UPDATEDIR = 0x00001000;
        private const uint SHCNF_PATH = 0x0005;
        private const uint SHCNF_IDLIST = 0x0000;

        /// <summary>
        /// 将自定义图标应用到文件夹
        /// </summary>
        /// <param name="folderPath">目标文件夹路径</param>
        /// <param name="iconPath">图标文件路径</param>
        /// <returns>是否成功应用图标</returns>
        public bool ApplyIconToFolder(string folderPath, string iconPath)
        {
            if (!Directory.Exists(folderPath))
            {
                Console.WriteLine($"❌ 文件夹不存在：{folderPath}");
                return false;
            }

            if (!File.Exists(iconPath))
            {
                Console.WriteLine($"❌ 图标文件不存在：{iconPath}");
                return false;
            }

            string desktopIniPath = Path.Combine(folderPath, "desktop.ini");

            // 使用绝对路径确保Windows能正确找到图标
            string absoluteIconPath = Path.GetFullPath(iconPath);

            // 构建desktop.ini内容 - 添加更多配置以提高兼容性
            string expectedContent =
                "[.ShellClassInfo]\r\n" +
                $"IconFile={absoluteIconPath}\r\n" +
                "IconIndex=0\r\n" +
                "ConfirmFileOp=0\r\n" +
                "[ViewState]\r\n" +
                "Mode=\r\n" +
                "Vid=\r\n" +
                "FolderType=Generic\r\n";

            try
            {
                // 第一步：准备文件夹和文件属性
                DirectoryInfo dirInfo = new DirectoryInfo(folderPath);
                dirInfo.Attributes &= ~FileAttributes.ReadOnly; // 移除只读属性

                // 第二步：处理现有desktop.ini
                if (File.Exists(desktopIniPath))
                {
                    File.SetAttributes(desktopIniPath, FileAttributes.Normal);
                    File.Delete(desktopIniPath); // 完全删除旧文件避免残留问题
                }

                // 第三步：创建并验证新文件
                File.WriteAllText(desktopIniPath, expectedContent);

                // 检查文件物理存在
                if (!File.Exists(desktopIniPath))
                {
                    throw new IOException("文件写入后未找到，可能磁盘写入失败");
                }

                // 第四步：验证文件内容
                string actualContent = File.ReadAllText(desktopIniPath);
                if (actualContent != expectedContent)
                {
                    File.Delete(desktopIniPath);
                    throw new InvalidDataException("文件内容校验失败，可能被安全软件拦截");
                }

                // 第五步：设置文件属性 - 先设置系统属性，再设置隐藏属性
                File.SetAttributes(desktopIniPath,
                    FileAttributes.System | FileAttributes.Hidden | FileAttributes.ReadOnly);

                // 第六步：设置文件夹属性 - 必须设置为系统文件夹
                dirInfo.Attributes |= FileAttributes.System;

                // 第七步：刷新系统缓存
                RefreshSystemIcons(folderPath);

                Console.WriteLine($"✅ 图标已成功应用到：{folderPath}");
                return true;
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.WriteLine($"⛔ 权限不足：{ex.Message}\n请尝试：\n1. 以管理员身份运行程序\n2. 关闭杀毒软件");
                return false;
            }
            catch (IOException ex)
            {
                Console.WriteLine($"🔧 IO操作异常：{ex.Message}\n可能原因：\n1. 文件被其他程序锁定\n2. 磁盘已满");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ 未知错误：{ex.GetType().Name} - {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 移除文件夹的自定义图标
        /// </summary>
        /// <param name="folderPath">目标文件夹路径</param>
        /// <returns>是否成功移除图标</returns>
        public bool RemoveFolderIcon(string folderPath)
        {
            if (!Directory.Exists(folderPath))
            {
                Console.WriteLine($"❌ 文件夹不存在：{folderPath}");
                return false;
            }

            string desktopIniPath = Path.Combine(folderPath, "desktop.ini");

            try
            {
                // 移除文件夹的系统属性
                DirectoryInfo dirInfo = new DirectoryInfo(folderPath);
                dirInfo.Attributes &= ~FileAttributes.System;
                dirInfo.Attributes &= ~FileAttributes.ReadOnly;

                // 如果desktop.ini存在，删除它
                if (File.Exists(desktopIniPath))
                {
                    File.SetAttributes(desktopIniPath, FileAttributes.Normal);
                    File.Delete(desktopIniPath);
                }

                // 刷新图标缓存
                RefreshSystemIcons(folderPath);

                Console.WriteLine($"✅ 已移除文件夹图标：{folderPath}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ 移除图标时出错：{ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 刷新系统图标缓存
        /// </summary>
        /// <param name="folderPath">需要刷新的文件夹路径</param>
        private void RefreshSystemIcons(string folderPath)
        {
            try
            {
                // 使用多种通知方式确保图标更新

                // 1. 通知文件夹更新
                SHChangeNotify(SHCNE_UPDATEDIR, SHCNF_PATH, folderPath, IntPtr.Zero);

                // 2. 通知项目更新
                SHChangeNotify(SHCNE_UPDATEITEM, SHCNF_PATH, folderPath, IntPtr.Zero);

                // 3. 通知文件关联变更
                SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_IDLIST, IntPtr.Zero, IntPtr.Zero);

                // 等待系统处理通知
                Thread.Sleep(300);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"刷新图标缓存时出错：{ex.Message}");
            }
        }

        /// <summary>
        /// 重启资源管理器（需要管理员权限）
        /// </summary>
        /// <returns>是否成功重启</returns>
        public bool RestartExplorer()
        {
            try
            {
                foreach (Process p in Process.GetProcessesByName("explorer"))
                {
                    p.Kill();
                }

                // 等待所有explorer进程终止
                Thread.Sleep(500);

                // 启动新的explorer进程
                Process.Start("explorer.exe");

                return true;
            }
            catch (Win32Exception ex)
            {
                Console.WriteLine($"需要管理员权限重启资源管理器：{ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"重启资源管理器时出错：{ex.Message}");
                return false;
            }
        }

        #endregion


        [RelayCommand]
        public void SelectAll() 
        {

         
        }
        [RelayCommand]
        public void DeSelectAll() 
        {
            
        }

        [RelayCommand]
        public void CheckBoxChecked(ProgramItem item)
        {

        }

        [RelayCommand]
        public void CheckBoxUnchecked(ProgramItem item)
        {
        }



    }
}

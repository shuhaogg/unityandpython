// PythonProcessController.cs
using UnityEngine;
using UnityEngine.UI;
using System.Diagnostics;
using System.IO;
using System.Text;
using static System.Net.Mime.MediaTypeNames;
using System;

public class PythonProcessController : MonoBehaviour
{
    [Header("配置")]
    public string pythonExePath = "D:/Program Files/anaconda1/python.exe";
    public bool showConsole = true;

    private Process uiProcess;

    void Start()
    {
        GetComponent<Button>()?.onClick.AddListener(LaunchAllProcesses);
    }

    public void LaunchAllProcesses()
    {
        string scriptsFolder = Path.Combine(UnityEngine.Application.streamingAssetsPath, "PythonScripts");
        string uiScriptPath = Path.Combine(scriptsFolder, "UI.py");
        string uiFilePath = Path.Combine(scriptsFolder, "多模信息采集系统.ui");

        if (!File.Exists(uiScriptPath))
        {
            UnityEngine.Debug.LogError($"找不到UI.py：{uiScriptPath}");
            return;
        }
        if (!File.Exists(uiFilePath))
        {
            UnityEngine.Debug.LogError($"找不到界面文件：{uiFilePath}");
            return;
        }

        // 关键修改：根据showConsole动态配置重定向
        var startInfo = new ProcessStartInfo
        {
            FileName = pythonExePath,
            Arguments = $"\"{uiScriptPath}\"",
            WorkingDirectory = scriptsFolder,
            UseShellExecute = showConsole,
            RedirectStandardOutput = !showConsole, // 仅在不显示控制台时重定向输出
            RedirectStandardError = !showConsole,
            CreateNoWindow = !showConsole
        };

        // 仅在重定向时设置编码
        if (!showConsole)
        {
            startInfo.StandardOutputEncoding = Encoding.UTF8;
            startInfo.StandardErrorEncoding = Encoding.UTF8;
        }

        try
        {
            uiProcess = new Process { StartInfo = startInfo };
            uiProcess.EnableRaisingEvents = true;

            // 仅在重定向时绑定事件
            if (!showConsole)
            {
                uiProcess.OutputDataReceived += (sender, e) =>
                    UnityEngine.Debug.Log($"Python输出：{e.Data}");
                uiProcess.ErrorDataReceived += (sender, e) =>
                    UnityEngine.Debug.LogError($"Python错误：{e.Data}");
            }

            uiProcess.Start();

            // 仅在重定向时开始读取
            if (!showConsole)
            {
                uiProcess.BeginOutputReadLine();
                uiProcess.BeginErrorReadLine();
            }

            UnityEngine.Debug.Log("成功启动Python进程");
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogError($"启动失败：{e.Message}");
        }
    }

    void OnApplicationQuit()
    {
        if (uiProcess != null && !uiProcess.HasExited)
        {
            try
            {
                uiProcess.Kill();
                uiProcess.WaitForExit(1000);
                uiProcess.Close();
            }
            catch { }
        }
    }
}
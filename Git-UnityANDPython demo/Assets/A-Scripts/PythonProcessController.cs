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
    [Header("����")]
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
        string uiFilePath = Path.Combine(scriptsFolder, "��ģ��Ϣ�ɼ�ϵͳ.ui");

        if (!File.Exists(uiScriptPath))
        {
            UnityEngine.Debug.LogError($"�Ҳ���UI.py��{uiScriptPath}");
            return;
        }
        if (!File.Exists(uiFilePath))
        {
            UnityEngine.Debug.LogError($"�Ҳ��������ļ���{uiFilePath}");
            return;
        }

        // �ؼ��޸ģ�����showConsole��̬�����ض���
        var startInfo = new ProcessStartInfo
        {
            FileName = pythonExePath,
            Arguments = $"\"{uiScriptPath}\"",
            WorkingDirectory = scriptsFolder,
            UseShellExecute = showConsole,
            RedirectStandardOutput = !showConsole, // ���ڲ���ʾ����̨ʱ�ض������
            RedirectStandardError = !showConsole,
            CreateNoWindow = !showConsole
        };

        // �����ض���ʱ���ñ���
        if (!showConsole)
        {
            startInfo.StandardOutputEncoding = Encoding.UTF8;
            startInfo.StandardErrorEncoding = Encoding.UTF8;
        }

        try
        {
            uiProcess = new Process { StartInfo = startInfo };
            uiProcess.EnableRaisingEvents = true;

            // �����ض���ʱ���¼�
            if (!showConsole)
            {
                uiProcess.OutputDataReceived += (sender, e) =>
                    UnityEngine.Debug.Log($"Python�����{e.Data}");
                uiProcess.ErrorDataReceived += (sender, e) =>
                    UnityEngine.Debug.LogError($"Python����{e.Data}");
            }

            uiProcess.Start();

            // �����ض���ʱ��ʼ��ȡ
            if (!showConsole)
            {
                uiProcess.BeginOutputReadLine();
                uiProcess.BeginErrorReadLine();
            }

            UnityEngine.Debug.Log("�ɹ�����Python����");
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogError($"����ʧ�ܣ�{e.Message}");
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
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Management;

namespace ListProcesses.Models;

public static class Extensions
{
    /// <summary>
    /// Extenstion of SimpleProcess, gets all child processes and parent process as a DetailedProcess
    /// </summary>
    /// <param name="process">SimpleProcess to work on</param>
    /// <returns></returns>
    public static IEnumerable<DetailedProcess> GetSystemProcesses(this SimpleProcess process)
    {
        List<(Process, ProcessType)> detailedProcesses = GatherProcesses(process.Id);
        foreach (var proc in detailedProcesses)
        {
            yield return new DetailedProcess(proc.Item1, proc.Item2);
        }
    }

    /// <summary>
    /// Extenstion of Process array, convert them all to SimpleProcesses
    /// </summary>
    /// <param name="processes">Process array converted to SimpleProcesses</param>
    /// <returns></returns>
    public static IEnumerable<SimpleProcess> GetSimpleProcesses(this Process[] processes)
    {
        foreach (var process in processes)
        {
            yield return new SimpleProcess(process);
        }
    }


    #region Utils
    /// <summary>
    /// Tries to get the process with the specified id, 
    /// gets all children of the process and its parrent 
    /// with their hierarchy in realation to the selected Process
    /// </summary>
    /// <param name="id">ID of the process to get children and parrent</param>
    /// <returns>List of Tuples of Process and its hierarchy</returns>
    public static List<(Process, ProcessType)> GatherProcesses(int id)
    {
        Process selected = null;
        List<(Process, ProcessType)> detailedProcesses;
        try
        {
            selected = Process.GetProcessById(id);
        }
        catch (Win32Exception){}
        catch (ArgumentException){}

        if (selected != null)
        {
            detailedProcesses = GetChildProcesses(selected);
            detailedProcesses.Add((selected, ProcessType.Selected));
            selected = GetParentProcess(selected);
            if (selected != null)
            {
                detailedProcesses.Add((selected, ProcessType.Parent));
            }
        }
        else
        {
            detailedProcesses = new List<(Process, ProcessType)> { (null, ProcessType.KilledOrUndefined) };
        }

        return detailedProcesses;
    }

    /// <summary>
    /// Get all children of the process using managment object searcher
    /// </summary>
    /// <param name="process"></param>
    /// <returns>List of Tuples of Process and its hierarchy</returns>
    public static List<(Process, ProcessType)> GetChildProcesses(Process process)
    {
        try
        {
            return new ManagementObjectSearcher($"Select ProcessID From Win32_Process Where ParentProcessID={process.Id}")
                .Get()
                .Cast<ManagementObject>()
                .Select(mo => (Process.GetProcessById(Convert.ToInt32(mo["ProcessID"])), ProcessType.Child))
                .ToList();
        }
        catch (Win32Exception)
        {
            return new List<(Process, ProcessType)>();
        }
        catch (ArgumentException)
        {
            return new List<(Process, ProcessType)>();
        }
    }

    /// <summary>
    /// Get parent of the process using managment object searcher
    /// </summary>
    /// <param name="process"></param>
    /// <returns>Parent process or null if not found</returns>
    public static Process GetParentProcess(Process process)
    {
        try
        {
            return new ManagementObjectSearcher($"Select ParentProcessID From Win32_Process Where ProcessID={process.Id}")
                .Get()
                .Cast<ManagementObject>()
                .Select(mo => Process.GetProcessById(Convert.ToInt32(mo["ParentProcessID"]))).FirstOrDefault();
        }
        catch (Win32Exception)
        {
            return null;
        }
        catch (ArgumentException)
        {
            return null;
        }
    }
    #endregion
}
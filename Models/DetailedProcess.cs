using System;
using System.ComponentModel;
using System.Diagnostics;

namespace ListProcesses.Models
{
    /// <summary>
    /// Enum defining hierarchyType of the process in the hierarchy
    /// </summary>
    public enum ProcessType
    {
        KilledOrUndefined,
        Parent,
        Selected,
        Child
    }

    /// <summary>
    /// Class representing one process
    /// </summary>
    public class DetailedProcess : IEquatable<DetailedProcess>
    {

        /// <summary>
        /// HierarchyType of undefined/parent/selected/child
        /// </summary>
        public ProcessType HierarchyType { get; private set; }

        /// <summary>
        /// Id of the process
        /// </summary>
        public int Id { get; private set; }

        /// <summary>
        /// Name of the process
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Title of the main window of the process or empty string
        /// </summary>
        public string WindowTitle { get; private set; }

        /// <summary>
        /// If the priorityClass class was accessed successfully without access violation
        /// </summary>
        public bool PriorityAccessed { get; private set; }

        /// <summary>
        /// PriorityClass class of the process or null if access violation
        /// </summary>
        public ProcessPriorityClass? PriorityClass { get; private set; }

        /// <summary>
        /// Constructor copying data from process instance with it's relation to the selected process
        /// </summary>
        /// <param name="process">Process converted to DetailedProcess</param>
        /// <param name="hierarchyType">Place in the current hierarchy</param>
        public DetailedProcess(Process process, ProcessType hierarchyType = ProcessType.KilledOrUndefined)
        {
            HierarchyType = hierarchyType;
            if (HierarchyType == ProcessType.KilledOrUndefined) return;

            try
            {
                Id = process.Id;
                Name = process.ProcessName;
                WindowTitle = process.MainWindowTitle;
            }
            catch (Win32Exception)
            {
                HierarchyType = ProcessType.KilledOrUndefined;
                PriorityNull();
                return;
            }
            catch (InvalidOperationException)
            {
                HierarchyType = ProcessType.KilledOrUndefined;
                PriorityNull();
                return;
            }

            try
            {
                PriorityClass = process.PriorityClass;
                PriorityAccessed = true;
            }
            catch (Win32Exception) { PriorityNull(); }
            catch (InvalidOperationException) { PriorityNull(); }
        }

        /// <summary>
        /// Try to change the priority of the process
        /// </summary>
        /// <param name="priority">Priority to change to</param>
        /// <returns>If changing of the priority was successful</returns>
        public bool ChangePriority(ProcessPriorityClass priority)
        {
            if (!PriorityAccessed) return false;
            try
            {
                var process = Process.GetProcessById(Id);
                if (priority == 0) priority = ProcessPriorityClass.Normal;
                process.PriorityClass = priority;
                PriorityClass = process.PriorityClass;
                return true;
            }
            catch (Win32Exception) {}
            catch (InvalidOperationException) {}
            return false;
        }

        /// <summary>
        /// Try to kill the process
        /// </summary>
        /// <returns>if the killing was successful</returns>
        public bool Kill()
        {
            if (!PriorityAccessed) return false;
            try
            {
                var process = Process.GetProcessById(Id);
                process.Kill();
                return true;
            }
            catch (Win32Exception) { }
            catch (InvalidOperationException) { }
            return false;
        }

        /// <summary>
        /// Assignment operator overload for Process objects
        /// </summary>
        /// <param name="process">Process converted to DetailedProcess</param>
        public static implicit operator DetailedProcess(Process process)
        {
            return new DetailedProcess(process);
        }

        /// <summary>
        /// Set priority to null
        /// </summary>
        private void PriorityNull()
        {
            PriorityClass = null;
            PriorityAccessed = false;
        }

        public bool Equals(DetailedProcess other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return HierarchyType == other.HierarchyType && Id == other.Id && Name == other.Name;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((DetailedProcess)obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine((int)HierarchyType, Id, Name);
        }
    }
}

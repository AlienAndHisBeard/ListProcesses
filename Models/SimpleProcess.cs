using System;
using System.ComponentModel;
using System.Diagnostics;

namespace ListProcesses.Models
{
    public class SimpleProcess : IEquatable<SimpleProcess>
    {
        /// <summary>
        /// Id of the process
        /// </summary>
        public int Id { get; private set; }

        /// <summary>
        /// Name of the process
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Create SimpleProcess from a Process
        /// </summary>
        /// <param name="process">Process converted to SimpleProcess</param>
        public SimpleProcess(Process process)
        {
            try
            {
                Id = process.Id;
                Name = process.ProcessName;
            }
            catch (Win32Exception) {}
            catch (InvalidOperationException){}
        }

        public bool Equals(SimpleProcess other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Id == other.Id && Name == other.Name;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((SimpleProcess)obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Id, Name);
        }
    }
}

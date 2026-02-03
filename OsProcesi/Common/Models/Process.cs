namespace Common.Models
{
    public class Process
    {
        public string Name
        {
            get => _name;
            set => _name = value;
        }
        
        
        
        public int ExecutionTime
        {
            get => _executionTime;
            set => _executionTime = value;
        }
        
        
        
        public int Priority
        {
            get => _priority;
            set => _priority = value;
        }
        
        
        
        public double CpuUsage
        {
            get => _cpuUsage;
            set => _cpuUsage = value;
        }
        
        
        
        public double MemoryUsage
        {
            get => _memoryUsage;
            set => _memoryUsage = value;
        }
        
        
        
        public Process(string name, int executionTime, int priority = 0, cpuUsage = 0, memoryUsage = 0.01)
        {
            Name = name;
            ExecutionTime = executionTime;
            Priority = priority;
            CpuUsage = cpuUsage;
            MemoryUsage = memoryUsage;
        }
        
        
        
        #region Private fields
        private string _name;
        private int    _executionTime;
        private int    _priority;
        private double _cpuUsage;
        private double _memoryUsage;
        #endregion
    }
}

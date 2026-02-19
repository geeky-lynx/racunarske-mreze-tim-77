namespace Common
{
    public static class Utilities
    {
        public static string PackProcessInfoForSending(IEnumerable<Common.Models.Process> processes)
        {
            if (processes.Count() <= 0)
                return "~";
            var first = processes.First();
            string infos = $"{first.Name}:{first.ExecutionTime}:{first.Priority}:{first.CpuUsage}:{first.MemoryUsage}";
            foreach (var process in processes.Skip(1))
                infos += $",{process.Name}:{process.ExecutionTime}:{process.Priority}:{process.CpuUsage}:{process.MemoryUsage}";
            return infos;
        }
    }
}

using System;
using System.IO;

namespace Baku.KinectForPepper
{
    class AngleLogger : IDisposable
    {
        public AngleLogger(ModelCore core)
        {
            _modelCore = core;
        }

        public void StartLogging(string filePath, bool allowFileOverWrite=false)
        {
            if (IsLogging)
            {
                return;
            }

            //上書きでデータ消されるのを基本的に防止すべし
            if(File.Exists(filePath) && !allowFileOverWrite)
            {
                throw new InvalidOperationException("Target file already exists");
            }

            _logWriter = new StreamWriter(filePath);
            _logWriter.WriteLine(string.Join(",", RobotJointAngles.AngleNames));

            _modelCore.AngleUpdated += OnAngleUpdated;
            IsLogging = true;

        }

        public void EndLogging()
        {
            if(!IsLogging)
            {
                return;
            }

            _modelCore.AngleUpdated -= OnAngleUpdated;
            _logWriter.Dispose();
            IsLogging = false;
        }

        public bool IsLogging { get; private set; }

        private readonly ModelCore _modelCore;
        private StreamWriter _logWriter;

        private void OnAngleUpdated(object sender, EventArgs e)
        {
            _logWriter?.WriteLine(string.Join(",", _modelCore.AngleOutputs));
        }

        public void Dispose() => EndLogging();
    }
}

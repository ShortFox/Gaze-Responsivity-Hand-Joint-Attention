namespace MQ.MultiAgent
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using UnityEngine;

    public sealed class ExpInitializer
    {
        private static readonly ExpInitializer _instance = new ExpInitializer();
        public static ExpInitializer Instance { get { return _instance; } }
        private string _filename = "ExpInitialization.csv";

        private ExpInitializer()
        {
            try
            {
                _initDirectory = new DirectoryInfo(Application.streamingAssetsPath);
                _initFile = _initDirectory.GetFiles(_filename);
                _initData = new List<string>();
                ReadFile(Enum.GetNames(typeof(InitHeader)));
            }
            catch
            {
                _available = false;
            }
        }

        #region Properties and Fields
        //Flag to denote if Initializer is available.
        private bool _available;
        public bool IsAvailable { get { return _available; } }
        //Flag to denote if Initializer went through file.
        private bool _finished;
        public bool IsFinished { get { return _finished; } } 

        private DirectoryInfo _initDirectory;       //Directory
        private FileInfo[] _initFile;               //File Information
        private string[] _initHeader;               //Header for INIT file.
        public string[] Header { get { return _initHeader; } }

        //Initialization Data with index.
        private List<string> _initData;
        private int _initIndx;
        private int InitIndx
        {
            get { return _initIndx; }
            set
            {
                if (value >= _initData.Count)
                {
                    _finished = true;
                }
                else { _initIndx =  value; }
            }
        }
        public int FileLength { get { return _initData.Count; } }
        public string[] CurrentItem { get { return _initData[InitIndx].Split(','); } }         //Current INIT file row.
        #endregion

        #region ExpInitializer Functions
        void ReadFile(string[] header_reference)
        {
            using (StreamReader sr = new StreamReader(_initFile[0].FullName))
            {
                while (!sr.EndOfStream)
                {
                    _initData.Add(sr.ReadLine());
                }
            }

            _initHeader = _initData[0].Split(',');
            _initData.RemoveAt(0);  //Remove header.
            _available = CheckCompatibility(header_reference);
            InitIndx = 0;
        }
        bool CheckCompatibility(string[] header_reference)
        {
            if (_initHeader.SequenceEqual(header_reference)) return true;
            else return false;
        }
        public string[] NextItem()
        {
            if (!_finished)
            {
                InitIndx++;
                return CurrentItem;
            }
            else return null;
        }
        public void SubmitError(string error_message)
        {
            Debug.LogError(error_message);
            _available = false;
        }
        public void ResetFile()
        {
            if (_available)
            {
                InitIndx = 0;
                _finished = false;
            }
        }
        #endregion
    }


    public class DataWriter
    {
        private string _subjID;
        private string _playerName;
        private string _directory;
        public List<string> Buffer;

        public DataWriter(int subjNum, string playerName)
        {
            CreateDirectory(subjNum, playerName);
            Buffer = new List<string>();
        }
        void CreateDirectory(int subjNum, string playerName)
        {
            _subjID = subjNum.ToString();
            _playerName = playerName;
            if (subjNum < 10)
            {
                _subjID = "0" + _subjID;
            }
            _subjID = "P" + _subjID;
            _directory = Application.dataPath + "/OutData/" + _subjID;

            if (!Directory.Exists(_directory))
            {
                Directory.CreateDirectory(_directory);
            }
        }
        public void FlushBuffer(DataHolder data, TrialInfo trial, int trialNumber)
        {
            if (Buffer.Count == 0) return;
            else
            {
                DateTime time = DateTime.Now;

                string filename = _subjID + "_" + _playerName + "_"+ trialNumber.ToString() + "_" + trial.Task + "_TrialID" + trial.TrialID.ToString() +"_Date_"+time.ToString("dd'_'MM'_'yyyy'_'H''mm''ss")+".csv";

                using (StreamWriter sw = File.CreateText(Path.Combine(_directory, filename)))
                {
                    sw.WriteLine(data.Header);

                    for (int i = 0; i < Buffer.Count; i++)
                    {
                        sw.WriteLine(Buffer[i]);
                    }
                }
                Buffer.Clear();
            }
        }
    }
}



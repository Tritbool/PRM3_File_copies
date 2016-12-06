using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace doublons
{

    class Passed {
        public String[] files { get; set; }
        public String dirPath { get; set; }

        public Passed(String[] files, String dirpath)
        {
            this.files = files;
            this.dirPath = dirpath;
        }

    }
    class Program
    {

        TextWriter writer;
        Dictionary<string, List<string>> myCollection;

        public Program()
        {
            myCollection = new Dictionary<string, List<string>>();
            writer = File.CreateText("doublons.txt");
        }

        public string[] subArray(string[] ss,int start, int end)
        {
            var size = end - start + 1;
            string[] s = new string[size];
            for (int i = start; i < end-start; i++) {
                s[i]= ss[i];
            }
            return s;
        }

        public byte[] toMD5(string f) { 
            var md5 = MD5.Create();
            var stream = File.OpenRead(f);
            return md5.ComputeHash(stream);
        }

        public string ByteArrayToString(byte[] ba)
        {
            StringBuilder hex = new StringBuilder(ba.Length * 2);
            foreach (byte b in ba)
                hex.AppendFormat("{0:x2}", b);
            return hex.ToString();
        }
        public bool cmpMD5(byte[] md51, byte[] md52) {
            return ByteArrayToString(md51) == ByteArrayToString(md52);
        }

        public void sort(string dirPath)
        {
            var fileEntries = Directory.GetFiles(dirPath).ToList();
            List<string[]> files1 = new List<string[]>();
            var i = 0;
            var cpus = Environment.ProcessorCount;
            var pack = fileEntries.Count / cpus;

            for (i = 0; i <= fileEntries.Count; i = i + pack)
            {
                Console.WriteLine(i);
                if(i+pack < fileEntries.Count)
                    files1.Add(fileEntries.GetRange(i, pack).ToArray());
                else
                    files1.Add(fileEntries.GetRange(i, fileEntries.Count-i).ToArray());

            }
            /*
            StreamWriter file1 =
            new StreamWriter("res.text");

            StreamWriter file2 =
            new StreamWriter("res1.text");

            Console.WriteLine("last index :" + (fileEntries.Count - i)+" size : "+fileEntries.Count + " i : "+i);


            foreach (string[] ss in files1) {
                Console.WriteLine(ss.Length);
                foreach(string s in ss)
                {
                    file1.WriteLine(s);
                }
            }
            foreach (string s in fileEntries) {
                file2.WriteLine(s);
            }

             */
            var threads = new List<Thread>();
            
            foreach (string[] ss in files1)
            {
                Thread t = new Thread(new ParameterizedThreadStart(step));
                var p = new Passed(ss, dirPath);
                threads.Add(t);
                t.Start(p);
                Console.WriteLine("launched");
                
            }
            var threadi = 0;
            foreach(Thread t in threads)
            {
                Console.WriteLine("Waiting for thread "+ threadi +" to end ");
                t.Join();
                threadi++;
            }
            
                ThreadStart threadDelegate = new ThreadStart(writeToFile);
            Thread tt = new Thread(threadDelegate);
            tt.Start();
        }

        void step(object data){
            var p = (Passed)data;
            var dirPath = p.dirPath;
            var files = p.files;
            foreach (string s in files)
            {
                var md51 = toMD5(s);
                string[] fileEntries2 = Directory.GetFiles(dirPath);
                var l = fileEntries2.ToList();
                l.Remove(s);

                foreach (string s1 in l)
                {
                    var md52 = toMD5(s1);
                    if (cmpMD5(md51, md52)) {
                        lock (((IDictionary)myCollection).SyncRoot)
                        {                  
                            List<string> list;
                            var stringMD5 = ByteArrayToString(md51);
                            if (!myCollection.TryGetValue(stringMD5, out list)) { 
                                list = new List<string>();
                                list.Add(s);
                                list.Add(s1);
                                myCollection.Add(stringMD5, list);
                                                                             
                            }
                            else{
                                
                                if (!list.Contains(s)){
                                    list.Add(s);
                                }
                                if (!list.Contains(s1))
                                {
                                    list.Add(s1);
                                }
                                
                            }
                            
                        }
                    }
                }
            }
        }


        void  writeToFile()
        {
            lock (((IDictionary)myCollection).SyncRoot)
            {
                int dbl = 0;
                TextWriter.Synchronized(writer).WriteLine("Il y a "+myCollection.Count+" fichiers doublonnés \n");
                foreach (KeyValuePair<string, List<string>> kvp in myCollection)
                {
                    TextWriter.Synchronized(writer).WriteLine("-----------------------------------------------------------------------------------------");
                    TextWriter.Synchronized(writer).WriteLine(kvp.Value.First() + ":\n");
                    kvp.Value.Remove(kvp.Value.First());
                    foreach (string s in kvp.Value)
                    {
                        TextWriter.Synchronized(writer).WriteLine("    -> " + s);
                        dbl++;
                    }
                    TextWriter.Synchronized(writer).WriteLine("-----------------------------------------------------------------------------------------");
                    TextWriter.Synchronized(writer).WriteLine("\n");
                }
                TextWriter.Synchronized(writer).WriteLine("Dont : " + dbl + " doublons directs");
                TextWriter.Synchronized(writer).Flush();
            }
        }

        static void Main(string[] args)
        {
            var prop = new Program();

			//give a dir path
            prop.sort(@"/path/to/directory");

            Console.ReadKey(true);
        }
    }
}

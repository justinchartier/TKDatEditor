using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DatEditor
{
    public partial class DatEditor : Form
    {
        public DatEditor()
        {
            InitializeComponent();
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                FileStream fs = new FileStream(openFileDialog1.FileName, FileMode.Open);
                BinaryReader br = new BinaryReader(fs);
                OpenDat(br);
                br.Close();
            }
        }
        private void button_add_Click(object sender, EventArgs e)
        {
            if (openFileDialog2.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                addFileName = openFileDialog2.SafeFileName;
                Add(openFileDialog2.FileName);
            }
        }
        private void button_remove_Click(object sender, EventArgs e)
        {
            if (listView.SelectedItems.Count > 0)
            {
                ListViewItem item = listView.SelectedItems[0];
                FileEntry file = FileEntries[item.Index];
                Remove(file);
            }
        }
        private void button_extract_Click(object sender, EventArgs e)
        {
            if (listView.SelectedItems.Count > 0)
            {
                ListViewItem item = listView.SelectedItems[0];
                saveFileDialog1.FileName = FileEntries[item.Index].Filename;
                if (saveFileDialog1.ShowDialog() == DialogResult.OK && saveFileDialog1.FileName != "")
                {
                    Extract(FileEntries[item.Index], saveFileDialog1.FileName);
                }
            }
        }
        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (saveFileDialog2.ShowDialog() == DialogResult.OK && saveFileDialog2.FileName != "")
            {
                Save(saveFileDialog2.FileName);
            }
        }

        public struct FileEntry
        {
            public uint StartAddress;
            public uint EndAddress;
            public uint Filesize;
            public string Filename;
            public byte[] data;
        }

        public FileEntry[] FileEntries;
        public string addFileName;

        public void Save(string filename)
        {
            BinaryWriter bw = new BinaryWriter(File.Open(filename, FileMode.Create));
            bw.Write(FileEntries.Count()+1);
            
            //write the header
            uint startAddress = (uint)(17 * (FileEntries.Count()+1)) + 4;
            uint writeAddress = startAddress;
            string name;
            byte[] buffer = new byte[13];
            for (int i = 0; i < FileEntries.Count(); i++)
            {
                name = FileEntries[i].Filename;
                buffer = ASCIIEncoding.ASCII.GetBytes(name);
                if (buffer.Count() < 13)
                {
                    int diff = 13 - buffer.Count();
                    Array.Resize<byte>(ref buffer, buffer.Count() + diff);
                }
                bw.Write(writeAddress);
                bw.Write(buffer);
                writeAddress = writeAddress + FileEntries[i].Filesize;
            }

            //write the null entry
            bw.Write(writeAddress);
            for (int i = 0; i < 13; i++)
            {
                bw.Write('\0');
            }

            //write the data
            for (int i = 0; i < FileEntries.Count(); i++)
            {
                bw.Write(FileEntries[i].data);
            }

            bw.Close();
        }
        public void ClearDat()
        {
            listView.Items.Clear();
            FileEntries = new FileEntry[0];
        }

        public void OpenDat(BinaryReader br)
        {
            ClearDat();
            uint totalCount;
            int i;
            byte[] fileNameBytes = new byte[13];

            totalCount = br.ReadUInt32();
            Console.WriteLine("totalCount is " + totalCount);
            FileEntries = new FileEntry[(totalCount - 1)];

            for (i = 0; i < (totalCount - 1); i++)
            {
                FileEntries[i].StartAddress = br.ReadUInt32();
                fileNameBytes = br.ReadBytes(13);
                string name = System.Text.Encoding.Default.GetString(fileNameBytes);
                FileEntries[i].Filename = name;
                FileEntries[i].EndAddress = br.ReadUInt32();
                
                FileEntries[i].Filesize = FileEntries[i].EndAddress - FileEntries[i].StartAddress;
                Console.WriteLine("-------BEGIN FILE " + (i+1) + "-------");
                Console.WriteLine("StartAddress is " + FileEntries[i].StartAddress);
                Console.WriteLine("Filename is " + FileEntries[i].Filename);
                Console.WriteLine("End address is " + FileEntries[i].EndAddress);
                Console.WriteLine("Filesize is " + FileEntries[i].Filesize);
                Console.WriteLine("-------END FILE " + (i + 1) + "-------");
                br.BaseStream.Seek(-4, SeekOrigin.Current);
            }

            for (i = 0; i < (totalCount - 1); i++)
            {
                br.BaseStream.Seek(FileEntries[i].StartAddress, SeekOrigin.Begin);
                FileEntries[i].data = br.ReadBytes((int)FileEntries[i].Filesize);
            }
            RefreshList();
        }

        public void Add(string filepath)
        {
            FileStream fs = new FileStream(filepath, FileMode.Open);
            BinaryReader br = new BinaryReader(fs);
            FileEntry[] newfiles = new FileEntry[(FileEntries.Count()+1)];
            int j = 0;
            for (int i = 0; i < FileEntries.Count(); i++)
            {
                newfiles[j] = FileEntries[i];
                j = j + 1;
            }
            newfiles[j].Filename = addFileName;
            newfiles[j].Filesize = (uint)fs.Length;
            newfiles[j].StartAddress = newfiles[(j - 1)].EndAddress;
            newfiles[j].EndAddress = newfiles[j].StartAddress + newfiles[j].Filesize;
            newfiles[j].data = br.ReadBytes((int)newfiles[j].Filesize);
            FileEntries = newfiles;

            ListViewItem item = new ListViewItem(FileEntries[j].Filename);
            item.SubItems.Add(FileEntries[j].Filesize.ToString() + " bytes");
            listView.Items.Add(item);

            br.Close();
        }
        public void Extract(FileEntry file, string savepath)
        {
            BinaryWriter bw = new BinaryWriter(File.Open(savepath, FileMode.Create));
            bw.Write(file.data);
            bw.Close();
        }

        public void Remove(FileEntry file)
        {
            
            FileEntry[] newfiles = new FileEntry[(FileEntries.Count()-1)];
            int j = 0;
            for (int i = 0; i < FileEntries.Count(); i++)
            {
                if (!FileEntries[i].Equals(file))
                {
                    newfiles[j] = FileEntries[i];
                    j = j + 1;
                }
            }
            FileEntries = newfiles;
            RefreshList();
        }

        public void RefreshList()
        {
            listView.Items.Clear();
            if (FileEntries.Count() > 0)
            {
                for (int i = 0; i < FileEntries.Count(); i++)
                {
                    ListViewItem item = new ListViewItem(FileEntries[i].Filename);
                    item.SubItems.Add(FileEntries[i].Filesize.ToString() + " bytes");
                    listView.Items.Add(item);
                }
            }
        }

        private void helpToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string caption = "About TK Dat Editor";
            string message = "TK Dat Editor v1.0\nWritten by Justin Chartier\n\nBased on Baram Dat Editor\nand information from Erik 'SiLo' Rogers\n\n2018";
            MessageBoxButtons buttons = MessageBoxButtons.OK;
            DialogResult result = MessageBox.Show(message, caption);
        }
    }
}
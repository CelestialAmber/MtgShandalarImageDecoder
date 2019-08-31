﻿/// Microprose MTG .PIC and .TR Viewer

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using System.IO;
using System.Runtime.InteropServices;

namespace ShandalarImageToolbox
{
 
    public partial class FormMain : Form
    {
        [DllImport("kernel32")]
        static extern bool AllocConsole();

        private List<Color[]> palettes = new List<Color[]>();
 

        public enum ImageType
        {
            Pic,
            Spr,
            Cat
        }
        public ImageType loadedImageType;
        public List<ShandalarAsset> loadedImages = new List<ShandalarAsset>();
        public int loadedImageIndex;
        public string windowTitle;
        public int selectedPaletteIndex;
        public FormMain()
        {
            

            InitializeComponent();

            windowTitle = Text;


#if DEBUG
            AllocConsole();
#endif
            SetupGrayPalette();

            paletteComboBox.Enabled = true;
            paletteComboBox.SelectedIndex = 0;
            paletteComboBox.Text = paletteComboBox.SelectedItem.ToString();
            previewModeComboBox.SelectedIndex = 0;
            previewModeComboBox.Text = previewModeComboBox.SelectedItem.ToString();
            

        }
        public void AddPalette(string name)
        {
            palettes.Add(new Color[256]);
            paletteComboBox.Items.Add(name);

        }
        public void AddPalette(Color[] palette, string name)
        {
            palettes.Add(palette);
            paletteComboBox.Items.Add(name);

        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        Random rnd = new Random();

        private Color RandomColor()
        {
            return Color.FromArgb(rnd.Next(256), rnd.Next(256), rnd.Next(256));
        }

        private Color GrayColor(byte value)
        {
            return Color.FromArgb(value, value, value);
        }

        private void SetupGrayPalette ()
        {
            AddPalette("Grayscale");
            for(int i=0; i<256; i++)
            {
                palettes[selectedPaletteIndex][i] = GrayColor((byte)i);
            }
        }

        /// <summary>
        /// Load and decode .PIC image
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void loadPicToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.DefaultExt = "pic";
            openFileDialog.Filter = "PIC Files|*.pic|All Files|*.*";
            if ( openFileDialog.ShowDialog() == DialogResult.OK)
            {

                
                byte[] fileData = File.ReadAllBytes(openFileDialog.FileName);
                string fileText = File.ReadAllText(openFileDialog.FileName);
                hexEditor1.LoadData(fileData);
                textBox1.Text = fileText;
                previewModeComboBox.SelectedIndex = 0;
                string loadedImageFilename = Path.GetFileNameWithoutExtension(openFileDialog.FileName);
                loadedImageType = ImageType.Pic;
                Text = windowTitle + " - " + Path.GetFileName(openFileDialog.FileName);
                Console.WriteLine("Loaded file path: " + openFileDialog.FileName);
                ShowPic(fileData, loadedImageFilename);
                assetsListBox.Items.Clear();
                assetsListBox.Items.Add(loadedImageFilename);
                exportToolStripMenuItem.Enabled = true;
                exportAllToolStripMenuItem.Enabled = true;
            }
        }

        private void ShowPic ( byte [] data, string name)
        {
            Color[] originalPalette = palettes[selectedPaletteIndex];
            int dataOffset = 0;

            string magic = Encoding.UTF8.GetString(data, 0, 2);
            dataOffset += 2;
            if (magic[0] == 'M')
            {
                bool is3fRange = magic == "M0";
                dataOffset += 2; //skip the palette data length value
                byte startIndex = data[dataOffset++];
                byte endIndex = data[dataOffset++];

                Color[] picFilePalette = new Color[256];
                for(int i = 0; i < endIndex - startIndex + 1; i++)
                {
                    int factor = is3fRange ? 4 : 1;
                    int r = factor * data[dataOffset++];
                    int g = factor * data[dataOffset++];
                    int b = factor * data[dataOffset++];

                    picFilePalette[i] = Color.FromArgb(r,g,b);
                }

                palettes[selectedPaletteIndex] = picFilePalette;
            }

            PicDecoder decoder = new PicDecoder(data);

            /// Output image as picture box

            Bitmap bitmap = new Bitmap(decoder.width, decoder.height);

           byte[,] imageData = new byte[decoder.width,decoder.height];
            decoder.DecodeImage(imageData);

            for (int y = 0; y < decoder.height; y++)
            {
                for (int x = 0; x < decoder.width; x++)
                {
                    byte value = imageData[x,y];
                    bitmap.SetPixel(x, y, palettes[selectedPaletteIndex][value]);

                }
            }
            palettes[selectedPaletteIndex] = originalPalette;
            ShandalarAsset asset = new ShandalarAsset(name, data);
            asset.image = bitmap;
            ClearImagePanel();
            loadedImages = new List<ShandalarAsset>();
            loadedImages.Add(asset);

        }

        private void LoadSprToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.DefaultExt = "spr";
            openFileDialog.Filter = "Sprite Files|*.spr|All Files|*.*";
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                exportToolStripMenuItem.Enabled = true;
                byte[] data = File.ReadAllBytes(openFileDialog.FileName);
                string fileText = File.ReadAllText(openFileDialog.FileName);
                hexEditor1.LoadData(data);
                textBox1.Text = fileText;
                previewModeComboBox.SelectedIndex = 0;
                Text = windowTitle + " - " + Path.GetFileName(openFileDialog.FileName);
                loadedImageType = ImageType.Spr;
                string loadedImageFilename = Path.GetFileNameWithoutExtension(openFileDialog.FileName);
                Console.WriteLine("Loaded file path: " + openFileDialog.FileName);
                List<Bitmap> sprites = SprDecoder.GetSprites(data, palettes[selectedPaletteIndex]).ToList();
                ClearImagePanel();
                loadedImages.Clear();
                for(int i = 0; i < sprites.Count; i++)
                {
                    ShandalarAsset asset = new ShandalarAsset(loadedImageFilename, data);
                    asset.image = sprites[i];
                    loadedImages.Add(asset);
                }
                assetsListBox.Items.Clear();
                for (int i = 0; i < loadedImages.Count; i++)
                {
                    assetsListBox.Items.Add(loadedImageFilename + "_" +i);
                }
                exportToolStripMenuItem.Enabled = true;
                exportAllToolStripMenuItem.Enabled = true;
                


            }

        }
        public void ShowImage(Bitmap imageTexture)
        {
            imagePanel.BackgroundImage = imageTexture;
            label2.Text = "Width: " + imageTexture.Width + "\n" + "Height: " + imageTexture.Height;
            if (imageTexture.Width > imagePanel.Width || imageTexture.Height > imagePanel.Height)
                imagePanel.BackgroundImageLayout = ImageLayout.Zoom;
            else
                imagePanel.BackgroundImageLayout = ImageLayout.Center;
        }
        public void ClearImagePanel()
        {
            imagePanel.BackgroundImage = null;
            label2.Text = "";
        }

        /// <summary>
        /// Load and setup palette
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void loadPaletteToolStripMenuItem_Click(object sender, EventArgs e)
        {

            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.DefaultExt = "tr";
            openFileDialog.Filter = "Palette Files|*.tr|All Files|*.*";
            openFileDialog.Multiselect = true;
            if ( openFileDialog.ShowDialog() == DialogResult.OK )
            {
                foreach (string fileName in openFileDialog.FileNames)
                {
                    string[] text = File.ReadAllLines(fileName);

                    PalDecoder decoder = new PalDecoder(text);

                    AddPalette(decoder.Palette,Path.GetFileNameWithoutExtension(fileName));
                    
                }

                
            }
        }

        private void viewPaletteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FormPalette paletteView = new FormPalette(palettes[selectedPaletteIndex]);

            paletteView.ShowDialog();
        }



        private void ToolStripSeparator1_Click(object sender, EventArgs e)
        {

        }

        private void exportPngToolStripMenuItem_Click(object sender, EventArgs e)
        {
            saveFileDialog1.FileName = Path.GetFileName(loadedImages[loadedImageIndex].filename);
            saveFileDialog1.InitialDirectory = Path.GetDirectoryName(loadedImages[loadedImageIndex].filename);
            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                 loadedImages[loadedImageIndex].image.Save(saveFileDialog1.FileName);

            }
        }
        private void ExportAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFolderDialog openFolderDialog = new OpenFolderDialog();
            openFolderDialog.InitialFolder = Path.GetDirectoryName(loadedImages[loadedImageIndex].filename);
            if (openFolderDialog.ShowDialog(this) == DialogResult.OK)
            {
               
                for (int i = 0; i < loadedImages.Count; i++)
                {
                    string imagesDirectory = openFolderDialog.SelectedFolder + "/extractedImages/" + loadedImages[i].filename;
                    if (!Directory.Exists(imagesDirectory)) Directory.CreateDirectory(imagesDirectory);
                    loadedImages[i].image.Save(imagesDirectory + "/" + loadedImages[i].filename + "_" + i + ".png");
                }

                Console.WriteLine("Finished exporting all images.");
            }

        }

        private void OpenFileDialog3_FileOk(object sender, CancelEventArgs e)
        {
            
        }

        private void OpenFileDialog2_FileOk(object sender, CancelEventArgs e)
        {

        }




        private void AssetsListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            loadedImageIndex = assetsListBox.SelectedIndex;
            if(loadedImageIndex != -1 && loadedImages.Count > 0 && loadedImages[assetsListBox.SelectedIndex].image != null) ShowImage(loadedImages[assetsListBox.SelectedIndex].image);

        }

        private void CATFileToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void LoadCATFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.DefaultExt = "cat";
            openFileDialog.Filter = "Cat Files|*.cat|All Files|*.*";
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                Cat cat = new Cat(openFileDialog.FileName);
                assetsListBox.Items.Clear();
                loadedImages.Clear();
                foreach(var file in cat.files)
                {
                    assetsListBox.Items.Add(file);
                }
            }
        }



        private void OpenFileDialog1_FileOk(object sender, CancelEventArgs e)
        {

        }

        private void PreviewModeComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            imagePanel.Visible = false;
            hexEditor1.Visible = false;
            textBox1.Visible = false;
            switch (previewModeComboBox.SelectedIndex)
            {
                case 0:
                    imagePanel.Visible = true;
                    break;
                case 1:
                    hexEditor1.Visible = true;
                    break;
                case 2:
                    textBox1.Visible = true;
                    break;
            }
        }

        private void PaletteComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            paletteComboBox.Text = paletteComboBox.SelectedItem.ToString();
            selectedPaletteIndex = paletteComboBox.SelectedIndex;
            if (loadedImages.Count > 0 && loadedImages[loadedImageIndex].data != null)
            {
                switch (loadedImageType)
                {
                    case ImageType.Pic:
                        ShowPic(loadedImages[loadedImageIndex].data, loadedImages[loadedImageIndex].filename);
                        ShowImage(loadedImages[loadedImageIndex].image);
                        break;
                    case ImageType.Spr:
                        List<Bitmap> sprites = SprDecoder.GetSprites(loadedImages[loadedImageIndex].data, palettes[selectedPaletteIndex]);
                        for (int i = 0; i < sprites.Count; i++)
                        {
                            ShandalarAsset asset = new ShandalarAsset(loadedImages[loadedImageIndex].filename, loadedImages[loadedImageIndex].data);
                            asset.image = sprites[i];
                            loadedImages[i] = asset;
                        }
                        ShowImage(loadedImages[loadedImageIndex].image);
                        break;


                }

            }
        }
    }

}
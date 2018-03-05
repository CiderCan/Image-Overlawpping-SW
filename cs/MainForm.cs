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
using OpenCvSharp;

namespace Elysium
{
    public partial class Form1 : Form
    {
        // The currently cropped image.
        private Bitmap CTImage;
        private Bitmap MRIImage;
        private Mat CTmat;
        private Mat MRImat;

        // The cropped image with the selection rectangle.
        private Bitmap DisplayImage;
        private Graphics DisplayGraphics;
        private int CTwidth = 200; // Default widht/CTheight = 200
        private int CTheight = 200;
        private int MRIwidth = 200; // Default widht/CTheight = 200
        private int MRIheight = 200;

        private String FileName;

        public Form1()
        {
            InitializeComponent();
        }

        // 불러오기 버튼 event
        private void button1_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog folderDlg = new FolderBrowserDialog();

            folderDlg.ShowNewFolderButton = true;

            // Show the FolderBrowserDialog.
            DialogResult result = folderDlg.ShowDialog();

            if (result == DialogResult.OK)
            {
                imgPath.Text = folderDlg.SelectedPath;

                Environment.SpecialFolder root = folderDlg.RootFolder;
            }
        }

        // 실행 버튼 event
        private void button2_Click(object sender, EventArgs e)
        {
            if (isImageRead() == false)
            {
                MessageBox.Show("폴더를 먼저 선택하세요.");
                return;
            }

            listBox1.Items.Clear();
            listBox2.Items.Clear();

            System.IO.DirectoryInfo di = new System.IO.DirectoryInfo(imgPath.Text + "//CT");
            foreach (System.IO.FileInfo File in di.GetFiles())
            {
                String FileNameOnly = File.Name;
                listBox1.Items.Add(FileNameOnly);
            }

            di = new System.IO.DirectoryInfo(imgPath.Text + "//MRI");
            foreach (System.IO.FileInfo File in di.GetFiles())
            {
                String FileNameOnly = File.Name;
                listBox2.Items.Add(FileNameOnly);
            }
        }

        // ListBox select event
        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Browser에서 받아온 File path 이용
            FileName = (String)listBox1.Items[listBox1.SelectedIndex];
            String MRIpath = imgPath.Text + "//MRI//";
            String CTpath = imgPath.Text + "//CT//";
            saveName.Text = imgPath.Text;

            if (File.Exists(MRIpath + FileName) && File.Exists(CTpath + FileName))
            {
                CTImage = Image.FromFile(CTpath + FileName) as Bitmap;
                CTImage = new Bitmap(CTImage, pictureBox1.Size);
                MRIImage = Image.FromFile(MRIpath + FileName) as Bitmap;
                MRIImage = new Bitmap(MRIImage, pictureBox2.Size);

                //CTImage = PreProcessing(OpenCvSharp.Extensions.BitmapConverter.ToMat(CTImage)); // 필요에 따라 추가

                this.pictureBox2.Image = MRIImage.Clone() as Bitmap;
                this.pictureBox1.Image = CTImage.Clone() as Bitmap;
            }
        }

        // PreProcessing Part
        private Bitmap PreProcessing(Mat img)
        {
            var element = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(5, 5), new OpenCvSharp.Point(0, 0));

            Cv2.Threshold(img, img, 100, 255, ThresholdTypes.Binary);
            Cv2.Erode(img, img, element);
            Cv2.Dilate(img, img, element);

            return OpenCvSharp.Extensions.BitmapConverter.ToBitmap(img);
        }

        private bool isImageRead()
        {
            if (this.imgPath.Text == "")
                return false;
            else
                return true;
        }

        // Let the user select an area.

        private System.Drawing.Point CTmp, CTsp, CTep;
        private System.Drawing.Point MRImp, MRIsp, MRIep;

        // Mouse Event begin
        private void pic1_MouseDown(object sender, MouseEventArgs e)
        {
            CTmp = e.Location;
           
            DrawPictureRange(1); // 이미지 Crop function

            this.pictureBox1.Invalidate();
        }

        private void pic2_MouseDown(object sender, MouseEventArgs e)
        {
            MRImp = e.Location;
           
            DrawPictureRange(2); // 이미지 Crop function

            this.pictureBox2.Invalidate();
        }
        //Mouse Event end


        // 오른쪽 하단 Overlay Image generator function
        private void OverlayImage()
        {
            CTmat = OpenCvSharp.Extensions.BitmapConverter.ToMat(picCropped.Image as Bitmap); // Bitmap to Mat
            MRImat = OpenCvSharp.Extensions.BitmapConverter.ToMat(picCropped2.Image as Bitmap);
            Mat resultMat = CTmat + MRImat; // 합연산 이미지 gen

            Bitmap result = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(resultMat);

            DisplayImage = new Bitmap(result, this.resultImage.Size);
            DisplayGraphics = Graphics.FromImage(DisplayImage);

            resultImage.Image = DisplayImage;
            resultImage.Refresh();
            resultImage.Invalidate();
        }


        // 이미지 저장 function
        private void saveImage(Bitmap origin)
        {
            string sDirPath;
            string[] pathName = { "Convert", "CT", "MRI", "Overlapping" };
            for (int i = 0; i < 4; i++)
            {
                sDirPath = (i == 0) ? imgPath.Text + "\\" + pathName[i] + "\\" : imgPath.Text + "\\Convert\\" + pathName[i] + "\\";
                DirectoryInfo di = new DirectoryInfo(sDirPath);
                if (di.Exists == false)
                {
                    di.Create();
                }
            }

            Mat img = OpenCvSharp.Extensions.BitmapConverter.ToMat(origin);

            if (img != null && CTmat != null && MRImat != null)
            {
                OpenCvSharp.Cv2.ImWrite(imgPath.Text + "\\Convert\\Overlapping\\" + FileName, img);
                OpenCvSharp.Cv2.ImWrite(imgPath.Text + "\\Convert\\CT\\" + FileName, CTmat);
                OpenCvSharp.Cv2.ImWrite(imgPath.Text + "\\Convert\\MRI\\" + FileName, MRImat);
            }

            CTmat = null;
            MRImat = null;
            img = null;
        }


        // 이미지 저장 button event
        private void button3_Click(object sender, EventArgs e)
        {
            saveImage(this.resultImage.Image as Bitmap);
        }

        // Crop Image 생성
        private void DrawCropImage(int x, int y, int width, int height, int type)
        {
            Rectangle source_rect = new Rectangle(x, y, width, height);
            Rectangle dest_rect = new Rectangle(0, 0, width, height);

            // if를 통해 CT와 MRI type에 따라 graphics 따로 지정
            if (type == 1)
            {
                DisplayImage = new Bitmap(width, height);
                DisplayGraphics = Graphics.FromImage(DisplayImage);
                DisplayGraphics.DrawImage(CTImage, dest_rect, source_rect, GraphicsUnit.Pixel);
                // Display the new bitmap.
                DisplayImage = new Bitmap(DisplayImage, picCropped.Size);
                DisplayGraphics = Graphics.FromImage(DisplayImage);
                picCropped.Image = DisplayImage;
                picCropped.Refresh();
            }
            else
            {
                DisplayImage = new Bitmap(width, height);
                DisplayGraphics = Graphics.FromImage(DisplayImage);
                DisplayGraphics.DrawImage(MRIImage, dest_rect, source_rect, GraphicsUnit.Pixel);
                // Display the new bitmap.
                DisplayImage = new Bitmap(DisplayImage, picCropped2.Size);
                DisplayGraphics = Graphics.FromImage(DisplayImage);
                picCropped2.Image = DisplayImage;
                picCropped2.Refresh();
            }

            if (picCropped.Image != null && picCropped2.Image != null)
            {
                OverlayImage();
            }
        }

        // CT 이미지 tracbar event
        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            CTwidth = 200;
            CTheight = 200;
            CTwidth += trackBar1.Value;
            CTheight += trackBar1.Value;
            DrawPictureRange(1);
        }

        // MRI 이미지 tracbar event
        private void trackBar3_Scroll(object sender, EventArgs e)
        {
            MRIwidth = 200;
            MRIheight = 200;
            MRIwidth += trackBar3.Value;
            MRIheight += trackBar3.Value;
            DrawPictureRange(2);
        }

        // Crop Range set event
        private void RangeSetting(System.Drawing.Point point, int width, int height, int type)
        {
            // if 분기점을 통해 CT와 MRI를 구별하여 event 실행
            if (type == 1)
            {
                CTsp.X = point.X - CTwidth / 2;
                CTep.X = point.X + CTwidth / 2;
                CTsp.Y = point.Y - CTheight;
                CTep.Y = point.Y;

                if (CTsp.X < 0) CTsp.X = 0;
                if (CTsp.X >= CTImage.Width) CTsp.X = CTImage.Width - 1;
                if (CTsp.Y < 0) CTsp.Y = 0;
                if (CTsp.Y >= CTImage.Height) CTsp.Y = CTImage.Height - 1;

                if (CTep.X < 0) CTep.X = 0;
                if (CTep.X >= CTImage.Width) CTep.X = CTImage.Width - 1;
                if (CTep.Y < 0) CTep.Y = 0;
                if (CTep.Y >= CTImage.Height) CTep.Y = CTImage.Height - 1;
            }
            else
            {
                MRIsp.X = point.X - MRIwidth / 2;
                MRIep.X = point.X + MRIwidth / 2;
                MRIsp.Y = point.Y - MRIheight;
                MRIep.Y = point.Y;

                if (MRIsp.X < 0) MRIsp.X = 0;
                if (MRIsp.X >= MRIImage.Width) MRIsp.X = MRIImage.Width - 1;
                if (MRIsp.Y < 0) MRIsp.Y = 0;
                if (MRIsp.Y >= MRIImage.Height) MRIsp.Y = MRIImage.Height - 1;

                if (MRIep.X < 0) MRIep.X = 0;
                if (MRIep.X >= MRIImage.Width) MRIep.X = MRIImage.Width - 1;
                if (MRIep.Y < 0) MRIep.Y = 0;
                if (MRIep.Y >= MRIImage.Height) MRIep.Y = MRIImage.Height - 1;
            }
        }

        // RangeSetting function과 DrawCropImage function을 이용하여 Graphics event를 수행. 실질적으로 이미지를 draw
        private void DrawPictureRange(int type)
        {
            if (type == 1)
            {
                System.Drawing.Point point = CTmp;
                //Graphics 설정
                DisplayGraphics = Graphics.FromImage(this.pictureBox1.Image);

                // Reset the image.
                DisplayGraphics.DrawImageUnscaled(CTImage, 0, 0);

                // Mouse Event를 통해 받은 Range를 받아옴
                RangeSetting(point, CTwidth, CTheight, 1);

                DisplayGraphics.DrawEllipse(Pens.Red, point.X - 2, point.Y - 2, 4, 4); // 기준점을 원으로 표시
                DisplayGraphics.DrawRectangle(Pens.Red, CTsp.X, CTsp.Y, CTwidth, CTheight); // Rectangle 생성
                DrawCropImage(CTsp.X, CTsp.Y, CTwidth, CTheight, 1); // 위의 과정을 거쳐 만들어진 Image를 해당 graphic object에 전송
                this.pictureBox1.Refresh();
            }
            else
            {
                System.Drawing.Point point = MRImp;
                //Graphics 설정
                DisplayGraphics = Graphics.FromImage(this.pictureBox2.Image);

                // Reset the image.
                DisplayGraphics.DrawImageUnscaled(MRIImage, 0, 0);

                // Mouse Event를 통해 받은 Range를 받아옴
                RangeSetting(point, MRIwidth, MRIheight, 2);

                DisplayGraphics.DrawEllipse(Pens.Red, point.X - 2, point.Y - 2, 4, 4); // 기준점을 원으로 표시
                DisplayGraphics.DrawRectangle(Pens.Red, MRIsp.X, MRIsp.Y, MRIwidth, MRIheight); // Rectangle 생성
                DrawCropImage(MRIsp.X, MRIsp.Y, MRIwidth, MRIheight, 2); // 위의 과정을 거쳐 만들어진 Image를 해당 graphic object에 전송
                this.pictureBox2.Refresh();
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Jai_FactoryDotNET;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.IO;
using System.Threading;


namespace JAI_Control
{
    public partial class Form1 : Form
    {
        [DllImport("OpenCVTest.dll", EntryPoint = "_startLedCheck", CallingConvention = CallingConvention.Cdecl)]
        static extern void _startLedCheck(byte[] ImageBuffer, int imageWedth, int imageHeight, int channel);

        [DllImport("OpenCVTest.dll", EntryPoint = "myImgPro", CallingConvention = CallingConvention.Cdecl)]
        static extern void myImgPro(byte[] ImageBuffer, int imageWedth, int imageHeight, int channel);

        CFactory myFactory = new CFactory();

        //打开摄像机对象
        CCamera myCam;

        //相机节点
        CNode myWidthNode;
        CNode myHeightNode;
        CNode myGainNode;

        public Form1()
        {
            InitializeComponent();

            Jai_FactoryWrapper.EFactoryError error = Jai_FactoryWrapper.EFactoryError.Success;

            //用默认寄存器基地址打开factory
            error = myFactory.Open("");

        }

        /// <summary>
        /// 打开相机
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button1_Click(object sender, EventArgs e)
        {
            if(null!=myCam)
            {
                //myCam.NewImageDelegate += new Jai_FactoryWrapper.ImageCallBack(HandleImage);
                //myCam.StartImageAcquisition(true, 5, pictureBox1.Handle);
                myCam.SkipImageDisplayWhenBusy = false;
                myCam.StartImageAcquisition(true, 5, pictureBox1.Handle);
                //myCam.StartImageAcquisition(true, 5);

                Thread th = new Thread(new ThreadStart(ThreadMethod)); //也可简写为new Thread(ThreadMethod);                
                th.Start(); //启动线程  

                //Jai_FactoryWrapper.RECT newRectSize;

                //newRectSize = new Jai_FactoryWrapper.RECT(0, 0, pictureBox1.Width, pictureBox1.Height);

                //Jai_FactoryWrapper.J_Image_ResizeChildWindow(myCam.WindowHandle, ref newRectSize);
            }
            

            //timer1.Enabled = true;
        }

        private void ThreadMethod()
        {
            myCam.NewImageDelegate += new Jai_FactoryWrapper.ImageCallBack(HandleImage);
        }     

        ColorPalette myMonoColorPalette = null;
        private void HandleImage(ref Jai_FactoryWrapper.ImageInfo ImageInfo)
        {
            //Jai_FactoryWrapper.ImageInfo myImg;
            //myImg = myCam.LastFrameCopy;

            Bitmap newImageBitmap = new Bitmap((int)ImageInfo.SizeX, (int)ImageInfo.SizeY,
                    (int)ImageInfo.SizeX, System.Drawing.Imaging.PixelFormat.Format8bppIndexed,
                    ImageInfo.ImageBuffer);

            // Create a Monochrome palette (only once)
            if (myMonoColorPalette == null)
            {
                Bitmap monoBitmap = new Bitmap(1, 1, PixelFormat.Format8bppIndexed);
                myMonoColorPalette = monoBitmap.Palette;

                for (int i = 0; i < 256; i++)
                    myMonoColorPalette.Entries[i] = Color.FromArgb(i, i, i);
            }

            for (int i = 0; i < 256; i++)
                myMonoColorPalette.Entries[i] = Color.FromArgb(i, i, i);

            // Set the Monochrome Color Palette
            newImageBitmap.Palette = myMonoColorPalette;

            //=====================================================
            //2018-09-11
            byte[] imgData;
            int stride;
            int ImageChannel = Image.GetPixelFormatSize(newImageBitmap.PixelFormat) / 8;
            getByteFrmBitmap(newImageBitmap, out imgData, out stride);
            //调用DLL图像处理
            //_startLedCheck(imgData, newImageBitmap.Width, newImageBitmap.Height, ImageChannel);
            //myImgPro(imgData, newImageBitmap.Width, newImageBitmap.Height, ImageChannel);
            //将图像处理之后的图像显示到C#的pictureBox中
            //pictureBox2.Image = getBitmapByBytes(imgData, newImageBitmap.Width,
            //    newImageBitmap.Height, stride, newImageBitmap.PixelFormat);

            //pictureBox2.Image = BytesToImage(imgData);
            //pictureBox2.Image = Image.FromStream(new MemoryStream(imgData));

            //==============================================
            //Bitmap bp = new Bitmap(newImageBitmap);
            //Bitmap bp = ToGrayBitmap(imgData, (int)ImageInfo.SizeX, (int)ImageInfo.SizeY);
            pictureBox2.Image = ToGrayBitmap(imgData, (int)ImageInfo.SizeX, (int)ImageInfo.SizeY);

            return;
        }
        /// <summary>  
        /// 将一个字节数组转换为8bit灰度位图  
        /// </summary>  
        /// <param name="rawValues">显示字节数组</param>  
        /// <param name="width">图像宽度</param>  
        /// <param name="height">图像高度</param>  
        /// <returns>位图</returns>  
        public static Bitmap ToGrayBitmap(byte[] rawValues, int width, int height)
        {
            //// 申请目标位图的变量，并将其内存区域锁定  
            Bitmap bmp = new Bitmap(width, height, PixelFormat.Format8bppIndexed);
            BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0, width, height),
             ImageLockMode.WriteOnly, PixelFormat.Format8bppIndexed);

            //// 获取图像参数  
            int stride = bmpData.Stride;  // 扫描线的宽度  
            int offset = stride - width;  // 显示宽度与扫描线宽度的间隙  
            IntPtr iptr = bmpData.Scan0;  // 获取bmpData的内存起始位置  
            int scanBytes = stride * height;// 用stride宽度，表示这是内存区域的大小  

            //// 下面把原始的显示大小字节数组转换为内存中实际存放的字节数组  
            int posScan = 0, posReal = 0;// 分别设置两个位置指针，指向源数组和目标数组  
            byte[] pixelValues = new byte[scanBytes];  //为目标数组分配内存  

            for (int x = 0; x < height; x++)
            {
                //// 下面的循环节是模拟行扫描  
                for (int y = 0; y < width; y++)
                {
                    pixelValues[posScan++] = rawValues[posReal++];
                }
                posScan += offset;  //行扫描结束，要将目标位置指针移过那段“间隙”  
            }

            //// 用Marshal的Copy方法，将刚才得到的内存字节数组复制到BitmapData中  
            System.Runtime.InteropServices.Marshal.Copy(pixelValues, 0, iptr, scanBytes);
            bmp.UnlockBits(bmpData);  // 解锁内存区域  

            //// 下面的代码是为了修改生成位图的索引表，从伪彩修改为灰度  
            ColorPalette tempPalette;
            using (Bitmap tempBmp = new Bitmap(1, 1, PixelFormat.Format8bppIndexed))
            {
                tempPalette = tempBmp.Palette;
            }
            for (int i = 0; i < 256; i++)
            {
                tempPalette.Entries[i] = Color.FromArgb(i, i, i);
            }

            bmp.Palette = tempPalette;

            //// 算法到此结束，返回结果  
            return bmp;
        }  

        //从Bitmap中获取图像数据并存入byte[]数组中
        private void getByteFrmBitmap(Bitmap bp, out byte[] rgbValues,out int stride)
        {
            Rectangle rect = new Rectangle(0, 0, bp.Width, bp.Height);
            System.Drawing.Imaging.BitmapData bmpData =
            bp.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadWrite, bp.PixelFormat);

            stride = bmpData.Stride;
            // Get the address of the first line.
            IntPtr ptr = bmpData.Scan0;

            // Declare an array to hold the bytes of the bitmap. 
            int bytes = Math.Abs(bmpData.Stride) * bp.Height;
            //byte[] rgbValues = new byte[bytes];
            rgbValues = new byte[bytes];

            // Copy the RGB values into the array.
            System.Runtime.InteropServices.Marshal.Copy(ptr, rgbValues, 0, bytes);

            // Unlock the bits.
            bp.UnlockBits(bmpData);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            if(null!=myCam)
            {
                if(myCam.IsOpen)
                {
                    myCam.Close();
                }
                myCam = null;
            }

            //寻找相机
            myFactory.UpdateCameraList(Jai_FactoryDotNET.CFactory.EDriverType.FilterDriver);

            //打开相机
            for(int i=0; i<myFactory.CameraList.Count;i++)
            {
                myCam = myFactory.CameraList[i];
                if(Jai_FactoryWrapper.EFactoryError.Success==myCam.Open())
                {
                    break;
                }
            }

            if(null!=myCam&&myCam.IsOpen)
            {
                myGainNode = myCam.GetNode("GainRaw");
                myHeightNode = myCam.GetNode("Height");
                myWidthNode = myCam.GetNode("Width");

                textBox1.Text = myGainNode.Value.ToString();
                textBox2.Text = myWidthNode.Value.ToString();
                textBox3.Text = myHeightNode.Value.ToString();
            }
            else
            {
                MessageBox.Show("No Cameras Found!");
            }
        }

        private void SetFramegrabberValue(String nodeName, Int64 int64Val)
        {
            if (null == myCam)
            {
                return;
            }

            IntPtr hDevice = IntPtr.Zero;
            Jai_FactoryWrapper.EFactoryError error = Jai_FactoryWrapper.J_Camera_GetLocalDeviceHandle(myCam.CameraHandle, ref hDevice);
            if (Jai_FactoryWrapper.EFactoryError.Success != error)
            {
                return;
            }

            if (IntPtr.Zero == hDevice)
            {
                return;
            }

            IntPtr hNode;
            error = Jai_FactoryWrapper.J_Camera_GetNodeByName(hDevice, nodeName, out hNode);
            if (Jai_FactoryWrapper.EFactoryError.Success != error)
            {
                return;
            }

            if (IntPtr.Zero == hNode)
            {
                return;
            }

            error = Jai_FactoryWrapper.J_Node_SetValueInt64(hNode, false, int64Val);
            if (Jai_FactoryWrapper.EFactoryError.Success != error)
            {
                return;
            }

            //Special handling for Active Silicon CXP boards, which also has nodes prefixed
            //with "Incoming":
            if ("Width" == nodeName || "Height" == nodeName)
            {
                string strIncoming = "Incoming" + nodeName;
                IntPtr hNodeIncoming;
                error = Jai_FactoryWrapper.J_Camera_GetNodeByName(hDevice, strIncoming, out hNodeIncoming);
                if (Jai_FactoryWrapper.EFactoryError.Success != error)
                {
                    return;
                }

                if (IntPtr.Zero == hNodeIncoming)
                {
                    return;
                }

                error = Jai_FactoryWrapper.J_Node_SetValueInt64(hNodeIncoming, false, int64Val);
            }
        }

        private void SetFramegrabberPixelFormat()
        {
            String nodeName = "PixelFormat";

            if (null == myCam)
            {
                return;
            }

            IntPtr hDevice = IntPtr.Zero;
            Jai_FactoryWrapper.EFactoryError error = Jai_FactoryWrapper.J_Camera_GetLocalDeviceHandle(myCam.CameraHandle, ref hDevice);
            if (Jai_FactoryWrapper.EFactoryError.Success != error)
            {
                return;
            }

            if (IntPtr.Zero == hDevice)
            {
                return;
            }

            long pf = 0;
            error = Jai_FactoryWrapper.J_Camera_GetValueInt64(myCam.CameraHandle, nodeName, ref pf);
            if (Jai_FactoryWrapper.EFactoryError.Success != error)
            {
                return;
            }
            UInt64 pixelFormat = (UInt64)pf;

            UInt64 jaiPixelFormat = 0;
            error = Jai_FactoryWrapper.J_Image_Get_PixelFormat(myCam.CameraHandle, pixelFormat, ref jaiPixelFormat);
            if (Jai_FactoryWrapper.EFactoryError.Success != error)
            {
                return;
            }

            StringBuilder sbJaiPixelFormatName = new StringBuilder(512);
            uint iSize = (uint)sbJaiPixelFormatName.Capacity;
            error = Jai_FactoryWrapper.J_Image_Get_PixelFormatName(myCam.CameraHandle, jaiPixelFormat, sbJaiPixelFormatName, iSize);
            if (Jai_FactoryWrapper.EFactoryError.Success != error)
            {
                return;
            }

            IntPtr hNode;
            error = Jai_FactoryWrapper.J_Camera_GetNodeByName(hDevice, nodeName, out hNode);
            if (Jai_FactoryWrapper.EFactoryError.Success != error)
            {
                return;
            }

            if (IntPtr.Zero == hNode)
            {
                return;
            }

            error = Jai_FactoryWrapper.J_Node_SetValueString(hNode, false, sbJaiPixelFormatName.ToString());
            if (Jai_FactoryWrapper.EFactoryError.Success != error)
            {
                return;
            }

            //Special handling for Active Silicon CXP boards, which also has nodes prefixed
            //with "Incoming":
            string strIncoming = "Incoming" + nodeName;
            IntPtr hNodeIncoming;
            error = Jai_FactoryWrapper.J_Camera_GetNodeByName(hDevice, strIncoming, out hNodeIncoming);
            if (Jai_FactoryWrapper.EFactoryError.Success != error)
            {
                return;
            }

            if (IntPtr.Zero == hNodeIncoming)
            {
                return;
            }

            error = Jai_FactoryWrapper.J_Node_SetValueString(hNodeIncoming, false, sbJaiPixelFormatName.ToString());
        }


        private void timer1_Tick(object sender, EventArgs e)
        {
            
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            //if (myCam != null) 
            //{
            //    myCam.StopImageAcquisition();
            //    //stopCaptureButton_Click(null, null);
            //    myCam.Close();
            //}
        }

        private void button3_Click(object sender, EventArgs e)
        {
            myCam.StopImageAcquisition();
            //stopCaptureButton_Click(null, null);
            myCam.Close();
        }

        
    }
}

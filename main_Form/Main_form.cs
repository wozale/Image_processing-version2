using AutoWindowsSize;
using Image_processing.Class;
using Image_processing.form;
using Image_processing.form.摄像头;
using Image_processing.main_Form;
using Newtonsoft.Json;
using OpenCvSharp;
using Sunny.UI;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using Point = OpenCvSharp.Point;

namespace Image_processing
{
    public partial class Main_form : UIForm
    {
        private new AutoAdaptWindowsSize? AutoSize;
        public linked_list link;
        public Mat img;
        public Mat mask;
        public static Mat? mat;//图片处理备份
        public static Data_List? data_List;
        private bool camera_open = false;
        private VideoCapture? VideoCapture;
        private Dictionary<string, del_process> Delegation_Deserialization;
        private Dictionary<del_process, string> Delegation_Serialization;

        #region 窗体加载

        public Main_form()
        {
            InitializeComponent();
            AutoSize = new AutoAdaptWindowsSize(this);
            link = new linked_list();//加载委托链表
            img = new Mat();
            mask = new Mat();
            mat = new Mat();
            data_List = new Data_List();
            Delegation_Deserialization = new Dictionary<string, del_process>()
            {
                {"colorto", OpenCV.colorto},
                {"medianBlur", OpenCV.medianBlur},
                {"boxFilter", OpenCV.boxFilter},
                {"Gaussian_Blur", OpenCV.Gaussian_Blur},
                {"Median_Blur", OpenCV.Median_Blur},
                {"Bilateral_Filter", OpenCV.Bilateral_Filter},
                {"X_Flip", OpenCV.X_Flip},
                {"Y_Flip", OpenCV.Y_Flip},
                {"XY_Flip", OpenCV.XY_Flip},
                {"ToBinary", OpenCV.ToBinary},
                {"AdaptiveThreshold", OpenCV.AdaptiveThreshold},
                {"Otsu", OpenCV.Otsu},
                {"Corrosion", OpenCV.Corrosion},
                {"Expansion", OpenCV.Expansion},
                {"Open_operation", OpenCV.Open_operation},
                {"Close_operation", OpenCV.Close_operation},
                {"Gradient_operation", OpenCV.Gradient_operation},
                {"Top_hat_operation", OpenCV.Top_hat_operation},
                {"Black_hat_operation", OpenCV.Black_hat_operation},
                {"Translation_rotation", OpenCV.Translation_rotation},
                {"Template_Match", OpenCV.Template_Match},
                {"Feature_Matching", OpenCV.Feature_Matching}
            };
            Delegation_Serialization = Delegation_Deserialization.ToDictionary(pair => pair.Value, pair => pair.Key);
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x02000000;
                return cp;
            }
        }

        private void Main_form_SizeChanged(object sender, EventArgs e)
        {
            if (AutoSize != null)
            {
                AutoSize.FormSizeChanged();
            }
        }

        private void Main_form_Load(object sender, EventArgs e)
        {
            if (Screen.PrimaryScreen != null)
            {
                // 设置窗体的位置为屏幕中央
                this.Location = new System.Drawing.Point((Screen.PrimaryScreen.WorkingArea.Width - this.Width) / 2,
                                       (Screen.PrimaryScreen.WorkingArea.Height - this.Height) / 2);
            }
            tree_add();//树状图加载
        }

        #endregion 窗体加载

        #region 工具栏

        private void open_pic_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < Application.OpenForms.Count; i++)
            {
                if (Application.OpenForms[i] != this)
                {
                    Application.OpenForms[i]?.Close();
                }
            }

            OpenFileDialog openFileDialog = new OpenFileDialog();
            //打开的文件选择对话框上的标题
            openFileDialog.Title = "请选择文件";
            //设置文件类型
            openFileDialog.Filter = "jpg图片|*.JPG|gif图片|*.GIF|png图片|*.PNG|jpeg图片|*.JPEG|BMP图片|*.BMP|MP4文件|*.mp4|所有文件|*.*";
            //设置默认文件类型显示顺序
            openFileDialog.FilterIndex = 1;
            //保存对话框是否记忆上次打开的目录
            openFileDialog.RestoreDirectory = false;
            //设置是否允许多选
            openFileDialog.Multiselect = false;
            //默认打开路径
            openFileDialog.InitialDirectory = @"F:\user\Pictures\Saved Pictures";
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                if (VideoCapture != null)
                {
                    VideoCapture.Dispose();
                    VideoCapture = null;
                }
                if (img != null)
                {
                    img.Dispose();
                }
                string path = openFileDialog.FileName;
                string ext = Path.GetExtension(path);
                if (ext == ".mp4" || ext == ".avi" || ext == ".mov")
                {

                    VideoCapture = new VideoCapture(path);

                    if (timer1.Enabled)
                    { timer1.Stop(); }
                    if (timer2.Enabled)
                    { timer2.Stop(); }
                    timer1.Interval = 1000 / 30;
                    timer1.Start();
                }
                else
                {
                    img = new Mat(path, ImreadModes.Color);
                    if (img.Empty())
                    {
                        MessageBox.Show("打开图片文件错误", "提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                    pictureBox1.Image = OpenCV.GetMat(img);
                }
            }
            return;
        }

        private void save_pic_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveImageDialog = new SaveFileDialog();
            saveImageDialog.Title = "图片保存";
            saveImageDialog.Filter = "jpg图片|*.jpg|gif图片|*.gif|png图片|*.png|jpeg图片|*.jpeg|BMP图片|*.bmp";//文件类型过滤,只可选择图片的类型
            saveImageDialog.FilterIndex = 1;//设置默认文件类型显示顺序
            saveImageDialog.FileName = "图片保存"; //设置默认文件名,可为空
            saveImageDialog.RestoreDirectory = true; //OpenFileDialog与SaveFileDialog都有RestoreDirectory属性,这个属性默认是false,
                                                     //打开一个文件后,那么系统默认目录就会指向刚才打开的文件。如果设为true就会使用系统默认目录
            saveImageDialog.InitialDirectory = @"F:\Pictures\Saved Pictures";
            if (saveImageDialog.ShowDialog() == DialogResult.OK)
            {
                string fileName = saveImageDialog.FileName.ToString();
                if (fileName != "" && fileName != null)
                {
                    string fileExtName = fileName.Substring(fileName.LastIndexOf(".") + 1).ToString();
                    System.Drawing.Imaging.ImageFormat? imgformat = null;
                    if (fileExtName != "")
                    {
                        switch (fileExtName)
                        {
                            #region 图片类型

                            case "jpg":
                                imgformat = System.Drawing.Imaging.ImageFormat.Jpeg;
                                break;

                            case "png":
                                imgformat = System.Drawing.Imaging.ImageFormat.Png;
                                break;

                            case "gif":
                                imgformat = System.Drawing.Imaging.ImageFormat.Gif;
                                break;

                            case "bmp":
                                imgformat = System.Drawing.Imaging.ImageFormat.Bmp;
                                break;

                            default:
                                imgformat = System.Drawing.Imaging.ImageFormat.Jpeg;
                                break;

                                #endregion 图片类型
                        }
                        MessageBox.Show("保存路径：" + fileName, "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        pictureBox1.Image.Save(fileName, imgformat);
                    }
                }
            }
        }

        private void open_Configuration_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            //打开的文件选择对话框上的标题
            openFileDialog.Title = "请选择文件";
            //设置文件类型
            openFileDialog.Filter = "Json文件|*.json";
            //设置默认文件类型显示顺序
            openFileDialog.FilterIndex = 1;
            //保存对话框是否记忆上次打开的目录
            openFileDialog.RestoreDirectory = false;
            //设置是否允许多选
            openFileDialog.Multiselect = false;
            //默认打开路径
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                string path = openFileDialog.FileName;
                using (var steamRead = new StreamReader(path))
                {
                    var settings = new JsonSerializerSettings();
                    settings.Converters.Add(new Data_ListJsonConverter());
                    string? str = steamRead.ReadLine();
                    data_List = JsonConvert.DeserializeObject<Data_List>(str, settings);
                    foreach (var item in data_List.Combobox_list)//加载listbox
                    {
                        listBox1.Items.Add(item.ToString());
                    }

                    foreach (var item in data_List.Serialization)//加载委托
                    {
                        link.AddDelegate(Delegation_Deserialization[item]);
                    }
                }
            }
        }

        private void save_Configuration_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveImageDialog = new SaveFileDialog();
            saveImageDialog.Title = "配置保存";
            saveImageDialog.Filter = "Json文件|*.json";//文件类型过滤,只可选择图片的类型
            saveImageDialog.FilterIndex = 1;//设置默认文件类型显示顺序
            saveImageDialog.FileName = "配置保存"; //设置默认文件名,可为空
            saveImageDialog.RestoreDirectory = true; //OpenFileDialog与SaveFileDialog都有RestoreDirectory属性,这个属性默认是false,
                                                     //打开一个文件后,那么系统默认目录就会指向刚才打开的文件。如果设为true就会使用系统默认目录
            if (saveImageDialog.ShowDialog() == DialogResult.OK)
            {
                string fileName = saveImageDialog.FileName;
                // 创建一个JsonSerializerSettings对象并添加CustomJsonConverter
                var settings = new JsonSerializerSettings();
                settings.Converters.Add(new Data_ListJsonConverter());

                foreach (var item in listBox1.Items)//将listbox里面的字符窜进行保存
                {
                    data_List.Combobox_list.Add(item.ToString());
                }
                foreach (del_process del in link.List?.GetInvocationList() ?? Enumerable.Empty<Delegate>())//将委托进行保存
                {
                    data_List.Serialization.Add(Delegation_Serialization[del]);
                }

                var json = JsonConvert.SerializeObject(data_List, settings);

                using (var steamwrite = new StreamWriter(fileName))
                {
                    steamwrite.WriteLine(json);
                }

            }
        }

        private void capture_Click(object sender, EventArgs e)
        {
            if (camera_open == false)
            {
                try
                {
                    Camera camera = new Camera();
                    camera.StartPosition = FormStartPosition.CenterScreen;
                    camera.ShowDialog();
                    if (camera.DialogResult == DialogResult.OK)
                    {
                        VideoCapture = new VideoCapture(camera.Cameras_Id);
                        if (timer1.Enabled)
                        { timer1.Stop(); }
                        if (timer2.Enabled)
                        { timer2.Stop(); }
                        timer1.Interval = 1000 / 30;
                        timer1.Start();
                        timer2.Interval = 1000 / camera.Camers_Frame_rate;
                        capture.Text = "关闭摄像头";
                        camera_open = true;
                    }
                }
                catch (Exception)
                {
                    MessageBox.Show("摄像头打开失败", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                VideoCapture.Dispose();
                VideoCapture = null;
                if (timer1.Enabled)
                { timer1.Stop(); }
                if (timer2.Enabled)
                { timer2.Stop(); }
                capture.Text = "打开摄像头";
                camera_open = false;
            }
        }

        /// <summary>
        /// 刷新图片
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void refresh_pic_Click(object sender, EventArgs e)
        {
            if (camera_open == false && img != null)
            {
                if (img.Empty())
                {
                    textBox1.AppendText("没有图片呀,处理什么呀！！！\r\n");
                    return;
                }
                Task.Run(() =>
                {
                    this.Invoke(new Action(() =>
                    {
                        uiWaitingBar1.Visible = true;
                    }));
                    Stopwatch sw = Stopwatch.StartNew();
                    mat = img.Clone();
                    int count = 0;
                    link.InvokeDelegates(ref mat, ref mask, ref count);
                    pictureBox1.Image?.Dispose();
                    pictureBox1.Image = OpenCV.GetMat(mat);
                    double time = sw.ElapsedMilliseconds;
                    this.Invoke(new Action(() =>
                    {
                        if (time / 1000 > 10)
                        {
                            time /= 1000.0;
                            toolStripStatusLabel1.Text = "图片处理用时：" + time.ToString() + " s";
                        }
                        else
                        {
                            toolStripStatusLabel1.Text = "图片处理用时：" + time.ToString() + " ms";
                        }
                        uiWaitingBar1.Visible = false;
                    }));
                });
            }
            else
            {
                if (timer1.Enabled)
                { timer1.Stop(); }
                if (timer2.Enabled)
                { timer2.Stop(); }
                timer2.Start();
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            using (var frame = new Mat())
            {
                VideoCapture.Read(frame);
                if (!frame.Empty())
                {
                    pictureBox1.Image?.Dispose();
                    pictureBox1.Image = OpenCV.GetMat(frame);
                }
            }
        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            using (var frame = new Mat())
            {
                VideoCapture.Read(frame);
                if (!frame.Empty())
                {
                    int count = 0;
                    Mat img = frame.Clone();
                    if (link.InvokeDelegates(ref img, ref mask, ref count))
                    {
                        pictureBox1.Image?.Dispose();
                        pictureBox1.Image = OpenCV.GetMat(img);
                        img.Dispose();
                        img = null;
                    }
                    else
                    {
                        timer2.Stop();
                    }
                }
            }
        }

        #endregion 工具栏

        #region PictureBox

        private void 查看图片信息ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Image img = pictureBox1.Image;
            if (img != null)//需要判断图片是否为空，已经是否被实例化
            {
                Mat mat = OpenCvSharp.Extensions.BitmapConverter.ToMat((Bitmap)img);
                MessageBox.Show("图片宽度：" + mat.Cols + "，图片高度：" + mat.Rows +
                    "，图片通道数：" + mat.Channels(), "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private enum Graphics_
        {
            矩形, 直线, 圆
        }

        private Graphics_ shape;

        public OpenCvSharp.Point ptStart = new OpenCvSharp.Point();
        public bool mouseDown = false;

        /// <summary>
        /// 计算PictyreBox中真实像素点位置
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        private Point True_coordinate_calculation(MouseEventArgs e, Mat mat)
        {
            float pic_x;//
            float pic_y;//图片在控件中的位置
            if (mat.Width > mat.Height)
            {
                float a = pictureBox1.Width / (float)mat.Width;
                pic_y = (e.Y - (pictureBox1.Height - (float)mat.Height * a) / 2) * 1 / a;
                pic_x = e.X * 1 / a;
            }
            else
            {
                float a = pictureBox1.Height / (float)mat.Height;
                pic_x = (e.X - (pictureBox1.Width - (float)mat.Width * a) / 2) * 1 / a;
                pic_y = e.Y * 1 / a;
            }
            return new Point(pic_x, pic_y);
        }

        private void 矩形绘制ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            shape = Graphics_.矩形;
        }

        private void 直线绘制ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            shape = Graphics_.直线;
        }

        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            Image img = pictureBox1.Image;
            if (img != null)
            {
                if (e.Button == MouseButtons.Left)
                {
                    mouseDown = true;
                    Mat mat = OpenCvSharp.Extensions.BitmapConverter.ToMat((Bitmap)img);
                    Point location = new Point();
                    location = True_coordinate_calculation(e, mat);
                    if (location.X < 0 || location.Y < 0 || location.X > mat.Width || location.Y > mat.Height)
                    {
                        return;
                    }
                    ptStart = True_coordinate_calculation(e, mat);
                    Vec3b bgr = mat.At<Vec3b>(ptStart.Y, ptStart.X);
                    byte blue = bgr[0];   // 蓝色通道值
                    byte green = bgr[1];  // 绿色通道值
                    byte red = bgr[2];    // 红色通道值
                    toolStripStatusLabel1.Text = "红色通道值：" + red + "，绿色通道值：" + green + "，蓝色通道值：" + blue;
                    mat.Dispose();
                    mat = null;
                    GC.Collect();
                }
            }
        }

        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            if (mouseDown)
            {
                if (e.Button == MouseButtons.Left)
                {
                    mat = img.Clone();

                    Point ptEnd = True_coordinate_calculation(e, mat);
                    if (shape == Graphics_.矩形)
                    {
                        Cv2.Rectangle(mat, ptStart, ptEnd, new Scalar(0, 0, 255), 2);
                    }
                    else if (shape == Graphics_.直线)
                    {
                        Cv2.Line(mat, ptStart, ptEnd, new Scalar(0, 0, 255), 2);
                        int distance = (int)Math.Sqrt(Math.Pow(ptEnd.X - ptStart.X, 2) + Math.Pow(ptEnd.Y - ptStart.Y, 2));
                        toolStripStatusLabel1.Text = "两点距离为：" + distance;
                    }
                    pictureBox1.Image?.Dispose();
                    pictureBox1.Image = OpenCV.GetMat(mat);
                    GC.Collect();
                }
            }
        }

        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                mouseDown = false;
            }
        }

        #endregion PictureBox

        #region ListBox

        private void 删除ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listBox1.SelectedItem != null)
            {

                while (listBox1.SelectedItems.Count > 0)
                {
                    object item = listBox1.SelectedItems[0];
                    string? list = item.ToString();
                    link.RemoveDelegateAt(listBox1.Items.IndexOf(item));
                    data_List.Data_list.RemoveAt(listBox1.Items.IndexOf(item));
                    listBox1.Items.Remove(item);
                    textBox1.AppendText(list + "删除成功\r\n");
                }

            }
        }

        private void 插入ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listBox1.SelectedItem != null)
            {
                Insert insert = new Insert();
                insert.StartPosition = FormStartPosition.CenterScreen;
                insert.ShowDialog();
                if (insert.DialogResult == DialogResult.OK)
                {
                    link.InsertDelegateAtPosition(insert._Process, listBox1.SelectedIndex);
                    change_set_parameter(insert._Name, "插入", listBox1.SelectedIndex);
                    listBox1.Items.Insert(listBox1.SelectedIndex, insert._Name);
                }
            }
        }


        private void 全选ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // 选中 ListBox 控件中的所有项
            for (int i = 0; i < listBox1.Items.Count; i++)
            {
                listBox1.SetSelected(i, true);
            }
        }

        /// <summary>
        /// 更改流程参数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void listBox1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (listBox1.SelectedItem != null)
            {
                change_parameter();
            }
        }

        /// <summary>
        /// 右键菜单
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void listBox1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && listBox1.SelectedItem != null)
            {
                int index = listBox1.IndexFromPoint(e.Location);
                if (index == ListBox.NoMatches)
                {
                    listBox1.ClearSelected();
                }
            }
            else if (e.Button == MouseButtons.Right && listBox1.SelectedItem != null)
            {
                int index = listBox1.IndexFromPoint(e.Location);
                if (index != ListBox.NoMatches)
                {
                    listbox_MenuStrip.Show(listBox1, e.Location);
                }
            }
        }

        #endregion ListBox

        #region tree

        private void 展开全部ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.treeView1.ExpandAll();
        }

        private void 折叠全部ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.treeView1.CollapseAll();
        }

        /// <summary>
        /// 选择方法
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void treeView1_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            switch_Method(e);
        }

        #endregion tree

    }
}
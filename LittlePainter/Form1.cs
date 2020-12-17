using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LittlePainter
{
    public partial class Form1 : Form
    {
        Graphics graph;
        Pen pen;
        Brush brush;
        Bitmap image, buffer;

        List<Point> points = new List<Point>();
        bool IsMouseDown;
        int PenStatus;//現在的畫筆是什麼型態
        List<Color> everyColor;//每次顏色
        Color currentBackColor;//現在顏色
        Color backColor;//要換的顏色
        int currentColorIndex = 0;
        private static int PenWidth = 1;//預設
        Point p;//現在游標位置
        int UndoCount = 0;
        int sideCount = 3;//正多邊形邊長
        List<Point> linesPoint = new List<Point>();//儲存這次的線

        List<Bitmap> Task;//儲存每次的圖
        int currentTaskIndex;
        Boolean stop = false;
        int transparency;//透明度

        int howpoint;
        double[] u;
        PointF[] BezierP;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

            image = new Bitmap(Canvas.Width, Canvas.Height);
            graph = Graphics.FromImage(image);

            graph.Clear(Color.White);
            pen = new Pen(Color.Black, PenWidth);
            brush = new SolidBrush(Color.Blue);
            currentBackColor = Color.White;
            everyColor = new List<Color>();
            everyColor.Add(currentBackColor);

            toolStripComboBox2.Text = "3";//正多邊形
            transparency = 0;//透明度
            IsMouseDown = false;
            toolStripComboBox3.Text = "1";//畫筆粗細

            //貝茲曲線初始化
            howpoint = 1000;
            u = new double[howpoint];
            BezierP = new PointF[howpoint];
            for (int i = 0; i < howpoint; i++)
            {
                u[i] = (double)i * (1.0 / (howpoint - 1));
            }

            Task = new List<Bitmap>();
            Task.Add(image);

            Canvas.Image = image;
        }

        private void Form1_SizeChanged(object sender, EventArgs e)
        {
            toolStripContainer1.Width = this.Size.Width-16;
            toolStripContainer1.Height = this.Size.Height - 67-statusStrip1.Height;
        }
        //判斷只能輸入整數
        private void toolStripComboBox3_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (Char)48 || e.KeyChar == (Char)49 ||
                e.KeyChar == (Char)50 || e.KeyChar == (Char)51 ||
                e.KeyChar == (Char)52 || e.KeyChar == (Char)53 ||
                e.KeyChar == (Char)54 || e.KeyChar == (Char)55 ||
                e.KeyChar == (Char)56 || e.KeyChar == (Char)57 ||
                e.KeyChar == (Char)13 || e.KeyChar == (Char)8 )
            {
                e.Handled = false; //取消輸入文字
            }
            else
            {
                e.Handled = true;  //允許輸入文字
            }
        }

        private void 儲存檔案ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                SaveFileDialog save = new SaveFileDialog();
                save.FileName = "DefaultName.jpg";
                save.Filter = "Jpeg Image|*.Jpeg";
                save.Title = "儲存為圖片檔";
                save.ShowDialog();
                
                if (save.FileName != "")
                {
                    System.IO.FileStream fs = (System.IO.FileStream)save.OpenFile();

                    switch (save.FilterIndex)
                    {
                        case 1:
                            this.Canvas.Image.Save(fs,
                                      System.Drawing.Imaging.ImageFormat.Jpeg);
                            break;
                    }

                    fs.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        
        void updateTask(Bitmap input)
        {
            currentTaskIndex++;
            if (UndoCount != 0)
            {
                for (int toDelete = currentTaskIndex, count = currentTaskIndex; toDelete < Task.Count; count++)
                {
                    Task.RemoveAt(toDelete);
                }
                UndoCount = 0;

            }
            Task.Add(input);
        }
        private void Canvas_MouseDown(object sender, MouseEventArgs e)
        {
            //上下邊框為1，做放大縮小時，位置重訂
            //FixedSingle = 1
            //Fixed3D = 2
            Point tempPoint = new Point(e.X* Canvas.Image.Width / (Canvas.Width - 2), e.Y* Canvas.Image.Height / (Canvas.Height - 2));
            IsMouseDown = true;
            points.Clear();
            points.Add(tempPoint);
            Canvas.Image = image;

            if(PenStatus==3||PenStatus==8||PenStatus==9){
                stop = false;
                if (e.Button == MouseButtons.Right)
                    stop = true;
                linesPoint.Add(tempPoint);
                if(PenStatus!=9)
                    updateTask(buffer);
                else{
                    buffer = new Bitmap(image);
                    Graphics g = Graphics.FromImage(buffer);

                    Point[] BControlPolygon;

                    BControlPolygon = new Point[linesPoint.Count + 1];
                    for (int i = 0; i < linesPoint.Count; i++)
                    {
                        BControlPolygon[i] = (Point)linesPoint[i];
                    }
                    BControlPolygon[linesPoint.Count] = p;
                    decasteljau(BControlPolygon);
                    g.DrawLines(pen, BezierP);

                    updateTask(buffer);
                }
            }
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            //上下邊框為1
            p = new Point(e.X * Canvas.Image.Width / (Canvas.Width - 2), e.Y * Canvas.Image.Height / (Canvas.Height - 2));

            buffer = new Bitmap(image); // 建立一個新的 buffer 緩衝區，畫的時候
            Graphics g = Graphics.FromImage(buffer);
            bool IsMouseRight = (e.Button == MouseButtons.Left);
            if(PenStatus==3||PenStatus==8||PenStatus==9){
                IsMouseRight = true;
                IsMouseDown = true;
            }
            if(e.X>0&&e.Y>0&&e.X<Canvas.Width&&e.Y<Canvas.Height&&!IsMouseDown){
                Color color = image.GetPixel(e.X, e.Y);
                color = image.GetPixel(e.X, e.Y);
                this.toolStripStatusLabel2.Text = "RGB(" + color.R + "," + color.G + "," + color.B + ")";
            }
            this.toolStripStatusLabel1.Text = "Pixel(" + e.X + "," + e.Y + ")";
            if (points.Count > 0 && IsMouseDown && IsMouseRight)
            {
                Point pStart = points[0];
                Point pLast = points[points.Count - 1];
                switch (PenStatus){
                    case 0://鉛筆
                        Point p0 = pStart;
                        foreach (Point p1 in points)
                        {
                            g.DrawLine(pen, p0, p1);
                            p0 = p1;
                        }
                        break;
                    case 1://矩形
                        //向右下
                        if (pStart.X < p.X && pStart.Y < p.Y){
                            g.FillRectangle(brush, pStart.X, pStart.Y, p.X - pStart.X, p.Y - pStart.Y);
                            g.DrawRectangle(pen, pStart.X, pStart.Y, p.X - pStart.X, p.Y - pStart.Y);
                        }
                        //向左上
                        if (pStart.X > p.X && pStart.Y > p.Y){
                            g.FillRectangle(brush, p.X, p.Y, pStart.X - p.X, pStart.Y - p.Y);
                            g.DrawRectangle(pen, p.X, p.Y, pStart.X - p.X, pStart.Y - p.Y);
                        }
                        //向左下
                        if (pStart.X > p.X && pStart.Y < p.Y){
                            g.FillRectangle(brush, p.X, pStart.Y, pStart.X - p.X, p.Y - pStart.Y);
                            g.DrawRectangle(pen, p.X, pStart.Y, pStart.X - p.X, p.Y - pStart.Y);
                        }
                        //向右上
                        if (pStart.X <= p.X && pStart.Y >= p.Y){
                            g.FillRectangle(brush, pStart.X, p.Y, p.X - pStart.X, pStart.Y - p.Y);
                            g.DrawRectangle(pen, pStart.X, p.Y, p.X - pStart.X, pStart.Y - p.Y);
                        }
                        break;
                    case 2://正方形
                        //右下 
                        if (pStart.X < p.X && pStart.Y < p.Y){
                            g.FillRectangle(brush, pStart.X, pStart.Y, p.Y - pStart.Y, p.Y - pStart.Y);
                            g.DrawRectangle(pen, pStart.X, pStart.Y, p.Y - pStart.Y, p.Y - pStart.Y);
                        }
                        //右上 
                        else if (pStart.X < p.X && pStart.Y >= p.Y){
                            g.FillRectangle(brush, pStart.X, p.Y, pStart.Y - p.Y, pStart.Y - p.Y);
                            g.DrawRectangle(pen, pStart.X, p.Y, pStart.Y - p.Y, pStart.Y - p.Y);
                        }
                        //左下
                        else if (pStart.X >= p.X && pStart.Y < p.Y){
                            g.FillRectangle(brush, p.X, pStart.Y, pStart.X - p.X, pStart.X - p.X);
                            g.DrawRectangle(pen, p.X, pStart.Y, pStart.X - p.X, pStart.X - p.X);
                        }
                        //左上
                        else if (pStart.X >= p.X && pStart.Y >= p.Y){
                            g.FillRectangle(brush, p.X, pStart.Y - (pStart.X - p.X), pStart.X - p.X, pStart.X - p.X);
                            g.DrawRectangle(pen, p.X, pStart.Y - (pStart.X - p.X), pStart.X - p.X, pStart.X - p.X);
                        }
                        break;
                    case 3://多邊形
                        if(stop!=true){
                            Point[] tempPolygonPointList = new Point[linesPoint.Count + 1];
                            for (int tp = 0; tp < linesPoint.Count; tp++)
                            {
                                tempPolygonPointList[tp] = linesPoint[tp];
                            }
                            tempPolygonPointList[linesPoint.Count] = p;
                            g.FillPolygon(brush, tempPolygonPointList);
                            g.DrawPolygon(pen, tempPolygonPointList);
                        }
                        break;
                    case 4://正圓
                        //向右下
                        if (pStart.X <= p.X && pStart.Y <= p.Y){
                            g.FillEllipse(brush, pStart.X, pStart.Y, p.X - pStart.X, p.X - pStart.X);
                            g.DrawEllipse(pen, pStart.X, pStart.Y, p.X - pStart.X, p.X - pStart.X);
                        }
                        //向左上
                        if (pStart.X >= p.X && pStart.Y >= p.Y){
                            g.FillEllipse(brush, pStart.X, pStart.Y, p.Y - pStart.Y, p.Y - pStart.Y);
                            g.DrawEllipse(pen, pStart.X, pStart.Y, p.Y - pStart.Y, p.Y - pStart.Y);
                        }
                        //向左下
                        if (pStart.X >= p.X && pStart.Y <= p.Y){
                            g.FillEllipse(brush, pStart.X, p.Y, pStart.Y - p.Y, pStart.Y - p.Y);
                            g.DrawEllipse(pen, pStart.X, p.Y, pStart.Y - p.Y, pStart.Y - p.Y);
                        }
                        //向右上
                        if (pStart.X <= p.X && pStart.Y >= p.Y){
                            g.FillEllipse(brush, p.X, pStart.Y, pStart.X - p.X, pStart.X - p.X);
                            g.DrawEllipse(pen, p.X, pStart.Y, pStart.X - p.X, pStart.X - p.X);
                        }
                        break;
                    case 5://橢圓
                        g.FillEllipse(brush, pStart.X, pStart.Y, p.X - pStart.X, p.Y - pStart.Y);
                        g.DrawEllipse(pen, pStart.X, pStart.Y, p.X - pStart.X, p.Y - pStart.Y);
                        break;
                    case 6://正多邊形
                        double[] center = { pStart.X, pStart.Y };
                        double radius = Math.Pow(((p.X - pStart.X) * (p.X - pStart.X) + (p.Y - pStart.Y) * (p.Y - pStart.Y)), 0.5);
                        // 每條邊對應的圓心角角度，精確為浮點數。使用弧度制，360度角為2派
                        double arc = 2 * Math.PI / sideCount;
                        // 為多邊形建立所有的頂點列表
                        Point[] pointList = new Point[sideCount];
                        for (int i = 0; i < sideCount; i++)
                        {
                            var curArc = arc * i; // 當前點對應的圓心角角度
                            // 就是簡單的三角函式正餘弦根據圓心角和半徑算點座標。這裡都取整就行
                            p.X = (int)(center[0] + Math.Round((radius * Math.Cos(curArc)), 2));
                            p.Y = (int)(center[1] + Math.Round((radius * Math.Sin(curArc)), 2));
                            pointList[i] = p;
                        }
                        g.FillPolygon(brush, pointList);
                        g.DrawPolygon(pen, pointList);
                        break;
                    case 7://直線
                        g.DrawLine(pen, pStart, p);
                        break;
                    case 8://連續直線
                        if(stop!=true){
                            Point[] tempPointList = new Point[linesPoint.Count + 1];
                            for (int tp = 0; tp < linesPoint.Count; tp++)
                            {
                                tempPointList[tp] = linesPoint[tp];
                            }
                            tempPointList[linesPoint.Count] = p;

                            g.DrawLines(pen, tempPointList);
                        }
                        break;
                    case 9://貝茲曲線
                        if (linesPoint.Count != 0)
                        {
                            Point[] BControlPolygon;

                            BControlPolygon = new Point[linesPoint.Count + 1];
                            for (int i = 0; i < linesPoint.Count; i++)
                            {
                                BControlPolygon[i] = (Point)linesPoint[i];
                            }
                            BControlPolygon[linesPoint.Count] = p;
                            decasteljau(BControlPolygon);
                            g.DrawLines(pen, BezierP);
                            g.DrawLines(new Pen(Color.Black), BControlPolygon);
                        }
                        break;
                    case 10://橡皮擦
                        Brush Eraser = new SolidBrush(currentBackColor);
                        int size = int.Parse(toolStripTextBox4.Text);
                        p0 = pStart;
                        foreach (Point p1 in points)
                        {
                            g.FillRectangle(Eraser, p1.X - size / 2, p1.Y - size / 2, size, size);
                            p0 = p1;
                        }
                        break;
                }
                points.Add(p);
            }
            Canvas.Image = buffer;
            Canvas.Update();
        }

        private void Canvas_MouseUp(object sender, MouseEventArgs e)
        {
            IsMouseDown = false;
            if(((PenStatus !=3)&&(PenStatus!=8)&&(PenStatus!=9))||stop)
            {
                image = buffer; // 將上次 MouseMove 畫的暫存結果取回 

                Canvas.Image = image;// 然後顯示出來

                linesPoint.Clear();

                if(!stop)
                    updateTask(image);
            }else{
                Canvas.Image = image;// 然後顯示出來
            }
        }

        private void 關閉檔案ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
        //放大
        private void toolStripLabel6_Click(object sender, EventArgs e)
        {
            Canvas.Height = (int)(Canvas.Size.Height * 1.1);
            Canvas.Width = (int)(Canvas.Size.Width * 1.1);
        }
        //縮小
        private void toolStripLabel7_Click(object sender, EventArgs e)
        {
            Canvas.Height = (int)(Canvas.Size.Height * 0.9);
            Canvas.Width = (int)(Canvas.Size.Width * 0.9);
        }
        //背景顏色
        private void toolStripButton4_Click(object sender, EventArgs e)
        {
            colorDialog1.FullOpen = true;
            colorDialog1.ShowHelp = true;
            if (colorDialog1.ShowDialog() == DialogResult.OK){
                toolStripButton4.BackColor = colorDialog1.Color;
                changeBackColor(colorDialog1.Color);
            }

        }
        //改變背景色
        private void changeBackColor(Color col){
            Bitmap temp = new Bitmap(image.Width, image.Height);
            backColor = col;
            for (int x = 0; x < image.Width; x++)
            {
                for (int y = 0; y < image.Height; y++)
                {
                    Color color = image.GetPixel(x, y);
                    if (color.R == currentBackColor.R && color.G == currentBackColor.G && color.B == currentBackColor.B)
                    {
                        temp.SetPixel(x, y, backColor);
                    }
                    else
                        temp.SetPixel(x, y, color);
                }
            }
            currentBackColor = backColor;
            everyColor.Add(currentBackColor);
            currentColorIndex++;

            image = temp;
            updateTask(image);
            Canvas.Image = image;
            Canvas.Update();

            Canvas.Refresh();
            this.Refresh();
        }
        //外框顏色
        private void toolStripButton5_Click(object sender, EventArgs e)
        {
            colorDialog1.FullOpen = true;
            colorDialog1.ShowHelp = true;
            if (colorDialog1.ShowDialog() == DialogResult.OK){
                pen.Color = colorDialog1.Color;
                toolStripButton5.BackColor = colorDialog1.Color;
            }
        }
        //內部顏色
        private void toolStripButton6_Click(object sender, EventArgs e)
        {
            colorDialog1.FullOpen = true;
            colorDialog1.ShowHelp = true;
            if (colorDialog1.ShowDialog() == DialogResult.OK)
            {
                brush = new SolidBrush(Color.FromArgb(255-transparency,colorDialog1.Color));
                toolStripButton6.BackColor = colorDialog1.Color;
            }
        }
        //畫筆粗細
        private void toolStripComboBox3_TextChanged(object sender, EventArgs e)
        {
            pen.Width = int.Parse(toolStripComboBox3.Text);
            toolStripComboBox1.Text = toolStripComboBox3.Text;
        }

        private void 縮放ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Form2 form2 = new Form2();
            form2.ShowDialog();
            if (form2.DialogResult == DialogResult.Yes){
                Canvas.Height = int.Parse(form2.textBox1.Text);
                Canvas.Width = int.Parse(form2.textBox2.Text);
            }
        }

        private void 開啟檔案ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFile = new OpenFileDialog();
            openFile.Title = "Open Image";
            openFile.FileName = "";

            if (DialogResult.OK == openFile.ShowDialog())
            {
                image = new Bitmap(openFile.FileName);
                Canvas.Height = image.Height;
                Canvas.Width = image.Width;
                Canvas.Image = image;
                updateTask(image);
                Canvas.Update();
            }
        }

        private void pencil_Click(object sender, EventArgs e)
        {
            PenStatus = 0;
            points.Clear();
            updateCheck();
            pencil.Checked = true;
            toolStripButton1.Checked = true;
        }

        private void Rectangle_Click(object sender, EventArgs e)
        {
            PenStatus = 1;
            points.Clear();
            updateCheck();
            Rectangle.Checked = true;
            矩形ToolStripMenuItem.Checked = true;
        }

        private void Square_Click(object sender, EventArgs e)
        {
            PenStatus = 2;
            points.Clear();
            updateCheck();
            Square.Checked = true;
            正方形ToolStripMenuItem.Checked = true;
        }

        private void Polygon_Click(object sender, EventArgs e)
        {
            PenStatus = 3;
            points.Clear();
            linesPoint.Clear();
            updateCheck();
            Polygon.Checked = true;
            多邊形ToolStripMenuItem.Checked = true;
        }

        private void Circle_Click(object sender, EventArgs e)
        {
            PenStatus = 4;
            points.Clear();
            updateCheck();
            Circle.Checked = true;
            圓形ToolStripMenuItem.Checked = true;
        }

        private void Ellipse_Click(object sender, EventArgs e)
        {
            PenStatus = 5;
            points.Clear();
            updateCheck();
            Ellipse.Checked = true;
            橢圓形ToolStripMenuItem.Checked = true;
        }

        private void RecPolygon_Click(object sender, EventArgs e)
        {
            PenStatus = 6;
            points.Clear();
            updateCheck();
            RecPolygon.Checked = true;
        }

        private void line_Click(object sender, EventArgs e)
        {
            PenStatus = 7;
            points.Clear();
            updateCheck();
            line.Checked = true;
            toolStripButton3.Checked = true;
        }

        private void lines_Click(object sender, EventArgs e)
        {
            PenStatus = 8;
            points.Clear();
            updateCheck();
            lines.Checked = true;
            toolStripButton10.Checked = true;
        }

        private void Bezier_Click(object sender, EventArgs e)
        {
            PenStatus = 9;
            points.Clear();
            updateCheck();
            Bezier.Checked = true;
            toolStripButton11.Checked = true;
        }

        private void 清除版面ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Bitmap temp = new Bitmap(image.Width, image.Height);
            for (int x = 0; x < image.Width; x++)
            {
                for (int y = 0; y < image.Height; y++)
                {
                    temp.SetPixel(x, y, Color.White);
                }
            }
            currentBackColor = Color.White;
            linesPoint.Clear();
            image = temp;
            updateTask(image);
            Canvas.Image = image;
            Canvas.Update();
            Canvas.Refresh();
            this.Refresh();
        }

        private void Eraser_Click(object sender, EventArgs e)
        {
            PenStatus = 10;
            points.Clear();
            updateCheck();
            Eraser.Checked = true;
            toolStripButton2.Checked = true;
        }
        //畫筆粗細(常用)
        private void toolStripComboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            pen.Width = int.Parse(toolStripComboBox1.Text);
            toolStripComboBox3.Text = toolStripComboBox1.Text;
        }
        //游標
        private void toolStripButton9_Click(object sender, EventArgs e)
        {
            updateCheck();
            toolStripButton9.Checked = true;
            toolStripButton12.Checked = true;
            PenStatus = -1;
            image = Task[currentTaskIndex]; // 將上次 MouseMove 畫的暫存結果取回
            Canvas.Image = image;// 然後顯示出來
            linesPoint.Clear();
        }

        private void undo(object sender, EventArgs e)
        {
            UndoCount++;
            if (currentTaskIndex != 0)
            { //只要不是只剩一張 都能夠復原 
                currentTaskIndex--;
                image = Task[currentTaskIndex];
                Canvas.Image = image;
            }
            if(currentColorIndex!=0)
            {
                currentColorIndex--;
                currentBackColor = everyColor[currentColorIndex];
                toolStripButton4.BackColor = currentBackColor;
            }
            Canvas.Update();
        }

        private void redo(object sender, EventArgs e)
        {
            if (currentTaskIndex + 1 < Task.Count)
            { //只要不是只剩一張 都能夠復原 
                currentTaskIndex++;
                image = Task[currentTaskIndex];
                Canvas.Image = image;
            }
            if(currentColorIndex + 1 < everyColor.Count)
            {
                currentColorIndex++;
                currentBackColor = everyColor[currentColorIndex];
                toolStripButton4.BackColor = currentBackColor;
            }
            Canvas.Update();
        }
        //正多邊形邊長修正
        private void toolStripComboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            sideCount = int.Parse(toolStripComboBox2.Text);
        }
        //貝茲曲線公式
        public void decasteljau(Point[] BCon)
        {
            int i, j, k, n;

            n = BCon.Length - 1;

            double[,] Q = new double[n + 1, n + 1];


            for (i = 0; i < n + 1; i++)
                for (j = 0; j < n + 1; j++)
                    Q[i, j] = 0.0;


            for (j = 0; j < n + 1; j++)
                Q[0, j] = (double)BCon[j].X;

            for (k = 0; k < howpoint; k++)
            {

                for (i = 1; i <= n; i++)
                    for (j = 0; j < n; j++)
                    {
                        Q[i, j] = (1.0 - u[k]) * Q[i - 1, j] + u[k] * Q[i - 1, j + 1];
                    }
                BezierP[k].X = (int)Q[n, 0];
            }
            for (j = 0; j < n + 1; j++)
                Q[0, j] = (float)BCon[j].Y;

            for (k = 0; k < howpoint; k++)
            {
                for (i = 1; i <= n; i++)
                    for (j = 0; j < n; j++)
                    {
                        Q[i, j] = (1.0 - u[k]) * Q[i - 1, j] + u[k] * Q[i - 1, j + 1];
                    }
                BezierP[k].Y = (int)Q[n, 0];
            }
        }

        private void 鉛筆ToolStripMenuItem_CheckedChanged(object sender, EventArgs e)
        {
            toolStripButton1.Visible = 鉛筆ToolStripMenuItem.Checked ? true : false;
        }

        private void 橡皮擦ToolStripMenuItem_CheckedChanged(object sender, EventArgs e)
        {
            toolStripButton2.Visible = 橡皮擦ToolStripMenuItem.Checked ? true : false;
        }

        private void 形狀ToolStripMenuItem_CheckedChanged(object sender, EventArgs e)
        {
            toolStripSplitButton1.Visible = 形狀ToolStripMenuItem.Checked ? true : false;
        }

        private void 直線ToolStripMenuItem_CheckedChanged(object sender, EventArgs e)
        {
            toolStripButton3.Visible = 直線ToolStripMenuItem.Checked ? true : false;
        }

        private void 多直線ToolStripMenuItem_CheckedChanged(object sender, EventArgs e)
        {
            toolStripButton10.Visible = 多直線ToolStripMenuItem.Checked ? true : false;
        }

        private void 貝茲曲線ToolStripMenuItem_CheckedChanged(object sender, EventArgs e)
        {
            toolStripButton11.Visible = 貝茲曲線ToolStripMenuItem.Checked ? true : false;
        }

        private void 灰階處理ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Bitmap temp = new Bitmap(image.Width, image.Height);
            for (int x = 0; x < image.Width; x++)
            {
                for (int y = 0; y < image.Height; y++)
                {
                    Color color = image.GetPixel(x, y);
                    //利用原本色彩產生不同灰色
                    int gray = (int)((color.R * 299 + color.G * 587 + color.B * 114 + 500) / 1000);
                    temp.SetPixel(x, y, Color.FromArgb(gray, gray, gray));//改變圖片的pixel
                }
            }

            image = temp;
            updateTask(image);

            Canvas.Image = image;
            Canvas.Update();
        }

        private void 負片處理ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Bitmap temp = new Bitmap(image.Width, image.Height);
            for (int x = 0; x < image.Width; x++)
            {
                for (int y = 0; y < image.Height; y++)
                {
                    Color color = image.GetPixel(x, y);
                    //反轉顏色
                    temp.SetPixel(x, y, Color.FromArgb(255 - color.R, 255 - color.G, 255 - color.B));
                }
            }
            image = temp;
            updateTask(image);//存到task，以便undo、redo用

            Canvas.Image = image;//存成背景圖
            Canvas.Update();//重繪
        }

        private void 紅色濾鏡ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Bitmap temp = new Bitmap(image.Width, image.Height);
            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    // 取得每一個 pixel
                    var pixel = image.GetPixel(x, y);
                    var pR = pixel.R + int.Parse(toolStripTextBox1.Text);
                    //判斷是否超過255 如果超過就是255
                    if (pR > 255) pR = 255;
                    if (pR < 0) pR = 0;
                    // 只寫入紅色的值 , G B 都放零
                    System.Drawing.Color newColor = System.Drawing.Color.FromArgb(pixel.A, pR, 0, 0);
                    temp.SetPixel(x, y, newColor);
                }
            }
            image = temp;
            updateTask(image);

            Canvas.Image = image;
            Canvas.Update();
        }

        private void 綠色濾鏡ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Bitmap temp = new Bitmap(image.Width, image.Height);
            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    // 取得每一個 pixel
                    var pixel = image.GetPixel(x, y);
                    var pG = pixel.G + int.Parse(toolStripTextBox2.Text);
                    //判斷是否超過255 如果超過就是255 
                    if (pG > 255) pG = 255;
                    if (pG < 0) pG = 0;
                    // 只寫入綠色的值 , R B 都放零
                    System.Drawing.Color newColor = System.Drawing.Color.FromArgb(pixel.A, 0, pG, 0);
                    temp.SetPixel(x, y, newColor);
                }
            }
            image = temp;
            updateTask(image);

            Canvas.Image = image;
            Canvas.Update();
        }

        private void 藍色濾鏡ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Bitmap temp = new Bitmap(image.Width, image.Height);
            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    // 取得每一個 pixel
                    var pixel = image.GetPixel(x, y);
                    var pB = pixel.B + int.Parse(toolStripTextBox3.Text);
                    //判斷是否超過255 如果超過就是255 
                    if (pB > 255) pB = 255;
                    if (pB < 0) pB = 0;
                    // 只寫入藍色的值 , R G 都放零
                    System.Drawing.Color newColor = System.Drawing.Color.FromArgb(pixel.A, 0, 0, pB);
                    temp.SetPixel(x, y, newColor);
                }
            }
            image = temp;
            updateTask(image);

            Canvas.Image = image;
            Canvas.Update();
        }

        private void 浮雕處理ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Bitmap temp = new Bitmap(image.Width, image.Height);

            for (int y = 0; y < image.Height - 1; y++)
            {
                for (int x = 0; x < image.Width - 1; x++)
                {
                    Color color = image.GetPixel(x, y);
                    // 先取得下一個 Pixel的顏色
                    Color nextColor = image.GetPixel(x, y + 1);
                    int r, g, b;
                    
                    r = Math.Abs(color.R - nextColor.R + 128);
                    r = r < 0 ? 0 : r;//防止小於0
                    r = r > 255 ? 255 : r;//防止大於255

                    g = Math.Abs(color.G - nextColor.G + 128);
                    g = g < 0 ? 0 : g;
                    g = g > 255 ? 255 : g;

                    b = Math.Abs(color.B - nextColor.B + 128);
                    b = b < 0 ? 0 : b;
                    b = b > 255 ? 255 : b;
                    temp.SetPixel(x, y, System.Drawing.Color.FromArgb(r, g, b));
                }
            }
            image = temp;
            updateTask(image);

            Canvas.Image = image;
            Canvas.Update();
        }

        private void 插入圖片ToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFile = new OpenFileDialog();
            openFile.Title = "Open Image";
            openFile.FileName = "";

            if (DialogResult.OK == openFile.ShowDialog())
            {
                Bitmap temp = new Bitmap(image.Width, image.Height);
                Bitmap original = image;//原本背景
                image = new Bitmap(openFile.FileName);
                //更改背景色
                for (int x = 0; x < Canvas.Width; x++)
                {
                    for (int y = 0; y < Canvas.Height; y++)
                    {
                        if (x >= image.Width || y >= image.Height)
                            backColor = currentBackColor;
                        else
                            backColor = image.GetPixel(x, y);//要換的顏色
                        Color color = original.GetPixel(x,y);//原本顏色
                        if (color.R == currentBackColor.R && color.G == currentBackColor.G && color.B == currentBackColor.B)
                        {
                            temp.SetPixel(x, y, backColor);
                        }
                        else
                            temp.SetPixel(x, y, color);
                    }
                }
                image = temp;
                updateTask(image);
                Canvas.Image = image;
                Canvas.Update();

                Canvas.Refresh();
                this.Refresh();
            }
        }
        //透明度
        private void toolStripTextBox5_TextChanged(object sender, EventArgs e)
        {
            if(toolStripTextBox5.Text!=""){
                int input = int.Parse(toolStripTextBox5.Text);
                if (input > 100)
                    toolStripTextBox5.Text = (input = input / 100).ToString();
                else
                    transparency = 255 * input/100;
                brush = new SolidBrush(Color.FromArgb(255 - transparency, toolStripButton6.BackColor));
            }
        }

        private void 說明ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Form3 form3 = new Form3();
            form3.ShowDialog();
        }

        private void updateCheck(){
            pencil.Checked = false;
            Rectangle.Checked = false;
            Square.Checked = false;
            Polygon.Checked = false;
            Circle.Checked = false;
            Ellipse.Checked = false;
            RecPolygon.Checked = false;
            line.Checked = false;
            lines.Checked = false;
            Bezier.Checked = false;
            Eraser.Checked = false;
            toolStripButton1.Checked = false;
            toolStripButton2.Checked = false;
            toolStripButton3.Checked = false;
            toolStripButton9.Checked = false;
            toolStripButton10.Checked = false;
            toolStripButton11.Checked = false;
            toolStripButton12.Checked = false;
            矩形ToolStripMenuItem.Checked = false;
            正方形ToolStripMenuItem.Checked = false;
            多邊形ToolStripMenuItem.Checked = false;
            圓形ToolStripMenuItem.Checked = false;
            橢圓形ToolStripMenuItem.Checked = false;
        }
        /*
*  0   鉛筆
*  1   長方形
*  2   正方形
*  3   多邊形
*  4   正圓
*  5   橢圓
*  6   正多邊形
*  7   直線
*  8   連續直線
*  9   貝茲曲線
*  10  橡皮擦
*  
*/
            }
        }

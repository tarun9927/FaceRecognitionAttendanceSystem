using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
using System.IO;
using MySql.Data.MySqlClient;
using System.Data;
using System.Diagnostics;

namespace MultiFaceRec
{
    public partial class FrmPrincipal : Form
    {
        //Declararation of all variables, vectors and haarcascades
        Image<Bgr, Byte> currentFrame;
        Capture grabber;
        HaarCascade face;
        HaarCascade eye;
        MCvFont font = new MCvFont(FONT.CV_FONT_HERSHEY_TRIPLEX, 0.5d, 0.5d);
        Image<Gray, byte> result, TrainedFace = null;
        Image<Gray, byte> gray = null;
        List<Image<Gray, byte>> trainingImages = new List<Image<Gray, byte>>();
        List<string> labels= new List<string>();
        List<string> NamePersons = new List<string>();
        int ContTrain, NumLabels, t;
        string name, names = null;

        private void take_attendance_Click(object sender, EventArgs e)
        {
            imageBoxFrameGrabber.Visible = false;
            panel1.Visible = false;
            panel2.Visible = false;
            imageBoxFrameGrabber.Visible = true;
            panel1.Visible = true;
            Boolean a = false;
            string connectionString = "Data Source = localhost; User ID = root; Password = toor123; Database=attendance; pooling = false; port = 3306; Allow User Variables = true; SslMode = none";
            MySqlConnection Conn = new MySqlConnection(connectionString);
            Conn.Open();
            if (Conn.State == ConnectionState.Open)
            {
                //MessageBox.Show("Connection is Active.");
                //konek.Close();
            }
            MySqlCommand command = Conn.CreateCommand();
            command.CommandType = CommandType.Text;
            command.CommandText = "SELECT COLUMN_NAME FROM Information_Schema.columns WHERE TABLE_SCHEMA='attendance';";
            DateTime date = DateTime.Now;
            MySqlDataReader Reader2;
            Reader2 = command.ExecuteReader();
            while (Reader2.Read())
            {
                string b = Convert.ToString(Reader2[0]);
                if (b == date.ToString("M/d/yy"))
                {
                    a = false;
                    break;
                }
                else
                {
                    a = true;
                }
            }
            Reader2.Close();
            Reader2.Dispose();
            if (a == true)
            {
                string add_column = "ALTER TABLE `students` ADD `" + date.ToString("M/d/yy") + "` VARCHAR(10) NOT NULL AFTER `Name`;";
                MySqlCommand cmd = Conn.CreateCommand();
                cmd.CommandText = add_column;
                Reader2 = cmd.ExecuteReader();
                Reader2.Close();
                Reader2.Dispose();
            }
            Conn.Close();
        }

        private void add_student_Click(object sender, EventArgs e)
        {
            imageBoxFrameGrabber.Visible = true;
            panel1.Visible = true;
            panel2.Visible = true;
        }

        private void Exit_Button_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        public FrmPrincipal()
        {
            InitializeComponent();
            //Load haarcascades for face detection
            face = new HaarCascade("haarcascade_frontalface_default.xml");
            //eye = new HaarCascade("haarcascade_eye.xml");
            imageBoxFrameGrabber.Visible = false;
            panel1.Visible = false;
            panel2.Visible = false;
            try
            {
                //Load of previus trainned faces and labels for each image
                string Labelsinfo = File.ReadAllText(Application.StartupPath + "/TrainedFaces/TrainedLabels.txt");
                string[] Labels = Labelsinfo.Split('%');
                NumLabels = Convert.ToInt16(Labels[0]);
                ContTrain = NumLabels;
                string LoadFaces;

                for (int tf = 1; tf < NumLabels+1; tf++)
                {
                    LoadFaces = "face" + tf + ".bmp";
                    trainingImages.Add(new Image<Gray, byte>(Application.StartupPath + "/TrainedFaces/" + LoadFaces));
                    labels.Add(Labels[tf]);
                }
            
            }
            catch(Exception e)
            {
               // MessageBox.Show("Nothing in binary database, please add at least a face(Simply train the prototype with the Add Face Button).", "Triained faces load", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }

        }


        private void button1_Click(object sender, EventArgs e)
        {
            //Initialize the capture device
            grabber = new Capture();
            grabber.QueryFrame();
            //Initialize the FrameGraber event
            Application.Idle += new EventHandler(FrameGrabber);
            button1.Enabled = false;
        }


        private void button2_Click(object sender, System.EventArgs e)
        {
            try
            {
                //Trained face counter
                ContTrain = ContTrain + 1;

                //Get a gray frame from capture device
                gray = grabber.QueryGrayFrame().Resize(320, 240, Emgu.CV.CvEnum.INTER.CV_INTER_CUBIC);

                MCvAvgComp[][] facesDetected = gray.DetectHaarCascade(
                face,
                1.2,
                10,
                Emgu.CV.CvEnum.HAAR_DETECTION_TYPE.DO_CANNY_PRUNING,
                new Size(20, 20));

                foreach (MCvAvgComp f in facesDetected[0])
                {
                    TrainedFace = currentFrame.Copy(f.rect).Convert<Gray, byte>();
                    break;
                }

                //resize face detected image for force for comparision
                TrainedFace = result.Resize(100, 100, Emgu.CV.CvEnum.INTER.CV_INTER_CUBIC);
                trainingImages.Add(TrainedFace);
                labels.Add(textBox1.Text);


                imageBox1.Image = TrainedFace;

                //Write the number of triained faces in a file text for further load
                File.WriteAllText(Application.StartupPath + "/TrainedFaces/TrainedLabels.txt", trainingImages.ToArray().Length.ToString() + "%");

                //Write the labels of triained faces in a file text for further load
                for (int i = 1; i < trainingImages.ToArray().Length + 1; i++)
                {
                    trainingImages.ToArray()[i - 1].Save(Application.StartupPath + "/TrainedFaces/face" + i + ".bmp");
                    File.AppendAllText(Application.StartupPath + "/TrainedFaces/TrainedLabels.txt", labels.ToArray()[i - 1] + "%");
                }

                MessageBox.Show(textBox1.Text + "´s face detected and added :)", "Training OK", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch
            {
                MessageBox.Show("Enable the face detection first", "Training Fail", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }


        void FrameGrabber(object sender, EventArgs e)
        {
            label3.Text = "0";
            //label4.Text = "";
            NamePersons.Add("");


            //Get the current frame form capture device
            currentFrame = grabber.QueryFrame().Resize(320, 240, Emgu.CV.CvEnum.INTER.CV_INTER_CUBIC);

                    //Convert it to Grayscale
                    gray = currentFrame.Convert<Gray, Byte>();

                    //Face Detector
                    MCvAvgComp[][] facesDetected = gray.DetectHaarCascade(
                  face,
                  1.2,
                  10,
                  Emgu.CV.CvEnum.HAAR_DETECTION_TYPE.DO_CANNY_PRUNING,
                  new Size(20, 20));

                    //Action for each element detected
                    foreach (MCvAvgComp f in facesDetected[0])
                    {
                        t = t + 1;
                        result = currentFrame.Copy(f.rect).Convert<Gray, byte>().Resize(100, 100, Emgu.CV.CvEnum.INTER.CV_INTER_CUBIC);
                        //draw the face detected in the 0th (gray) channel with blue color
                        currentFrame.Draw(f.rect, new Bgr(Color.Red), 2);


                        if (trainingImages.ToArray().Length != 0)
                        {

                        MCvTermCriteria termCrit = new MCvTermCriteria(ContTrain, 0.001);

                        EigenObjectRecognizer recognizer = new EigenObjectRecognizer(
                           trainingImages.ToArray(),
                           labels.ToArray(),
                           3000,
                           ref termCrit);

                        name = recognizer.Recognize(result);

                        currentFrame.Draw(name, ref font, new Point(f.rect.X - 2, f.rect.Y - 2), new Bgr(Color.LightGreen));

                        }

                            NamePersons[t-1] = name;
                            NamePersons.Add("");


                        label3.Text = facesDetected[0].Length.ToString();

                    }
                        t = 0;

                    for (int nnn = 0; nnn < facesDetected[0].Length; nnn++)
                    {
                        names = names + NamePersons[nnn] + ", ";
                        if(panel2.Visible==false)
                        {
                            string connectionString = "Data Source = localhost; User ID = root; Password = toor123; Database=attendance; pooling = false; port = 3306; Allow User Variables = true; SslMode = none";
                            MySqlConnection Conn = new MySqlConnection(connectionString);
                            Conn.Open();
                            if (Conn.State == ConnectionState.Open)
                            {
                                MySqlDataReader Reader2;
                                //konek.Close();
                                DateTime date = DateTime.Now;
                                string mark_attendance = "UPDATE `students` SET `" + date.ToString("M/d/yy") + "` = 'P' WHERE `students`.`Name` = '"+ NamePersons[nnn] + "';";
                                MySqlCommand cmd2 = Conn.CreateCommand();
                                cmd2.CommandText = mark_attendance;
                                Reader2 = cmd2.ExecuteReader();
                                Reader2.Close();
                                Reader2.Dispose();
                            }
                            Conn.Close();
                        }
                    }
                    imageBoxFrameGrabber.Image = currentFrame;
                    label4.Text = names;
                    names = "";
                    NamePersons.Clear();

                }


    }
}
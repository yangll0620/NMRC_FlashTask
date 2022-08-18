using System;
using System.Windows;
using System.Collections.Generic;
using swf = System.Windows.Forms;
using sd = System.Drawing;
using System.Windows.Shapes;
using System.Windows.Media;

namespace FlashTask_wpf
{
    class Utility
    {
        static public int ratioIn2Pixal = 96;
        static public float ratioCM2Pixal = (float)96 / (float)2.54;

        public Utility()
        { }


        public static swf.Screen Detect_oneNonPrimaryScreen()
        {/* Detect the first not Primary Screen */

            swf.Screen[] screens = swf.Screen.AllScreens;
            swf.Screen nonPrimaryS = swf.Screen.PrimaryScreen; 
            foreach (swf.Screen s in screens)
            {
                if(s.Primary == false)
                {
                    nonPrimaryS = s;
                    break;
                }
            }
            return nonPrimaryS;
        }

        public static sd.Rectangle Detect_PrimaryScreen_Rect()
        {
            swf.Screen PrimaryS = swf.Screen.PrimaryScreen;
            sd.Rectangle screenRect = PrimaryS.Bounds;

            return screenRect;
        }

        public static int cm2pixal(float cmlen)
        {/* convert length with unit cm to unit pixal, 96 pixals = 1 inch = 2.54 cm

            args:   
                cmlen: to be converted length (unit: cm)

            return:
                pixalen: converted length with unit pixal
         */

            int pixalen = (int)(cmlen * ratioCM2Pixal);

            return pixalen;
        }

        public static int in2pixal(float inlen)
        {/* convert length with unit inch to unit pixal, 96 pixals = 1 inch = 2.54 cm

            args:   
                cmlen: to be converted length (unit: inch)

            return:
                pixalen: converted length with unit pixal
         */     

            int pixalen = (int)(inlen * ratioIn2Pixal);

            return pixalen;
        }


        public static List<int[]> GenRandomPositions(int n, int epilson, sd.Rectangle workArea)
        {/*
                Generate the optional X, Y Positions (origin in center) for workArea
                Unit is pixal

                Args:
                    n: the number of generated positions
                    epilson: x range in [-width/2 + epilson,  width/2 - epilson] 
                             y range in [-height/2 + epilson,  height/2 - epilson]

            */

            List<int[]> optPostions_OCenter_List = new List<int[]>();

            
            int xMin = -workArea.Width / 2 + epilson, xMax = workArea.Width / 2 - epilson;
            int yMin = -workArea.Height / 2 + epilson, yMax = workArea.Height / 2 - epilson;

            // generate randomly x, y positions
            Random rnd = new Random();     
            for (int i = 0; i < n; i++)
            {
                int x = rnd.Next(0, xMax - xMin) + xMin;
                int y = rnd.Next(0, yMax - yMin) + yMin;
                optPostions_OCenter_List.Add(new int[] { x, y });
            }

            return optPostions_OCenter_List;
        }



        public static List<int[]> GenDefaultPositions(int n, int radius, sd.Rectangle workArea)
        {/*
                Generate the default optional X, Y Positions (origin in center) for workArea
                1. The first position always [0, 0]
                2. The remaining equally in a circle (origin = [0, 0], radius)

                Unit is pixal

                Args:
                    n: the number of generated positions
                    radius: the radius of the circle (Pixal)

            */
            List<int[]> defaultPostions_OCenter_List = new List<int[]>();


            if(n >= 1) 
            {// the first position always (0, 0)
                defaultPostions_OCenter_List.Add(new int[] { 0, 0 });
            }
            

            // generate x, y positions randomly in a circle
            for (int i = 2; i <= n; i++)
            {
                double deg = 2 * Math.PI / (n - 1) * i;
                int x = (int) (radius * Math.Cos(deg)), y = (int)(radius * Math.Sin(deg));
                defaultPostions_OCenter_List.Add(new int[] { x, y });
            }

            return defaultPostions_OCenter_List;

        }


            public static Ellipse Create_Circle(double Diameter, SolidColorBrush brush_Fill)
        {/*
            Create the circle

            Args:
                Diameter: the Diameter of the Circle in Pixal

            */

            // Create an Ellipse  
            Ellipse circle = new Ellipse();

            // set the size, position of circleGo
            circle.Height = Diameter;
            circle.Width = Diameter;

            circle.Fill = brush_Fill;

            return circle;
        }


        public static Ellipse Move_Circle_OTopLeft(Ellipse circle, int[] cPoint_Pos_OTopLeft)
        {/*
            Move the circle into cPoint_Pos_OTopLeft (Origin in the topLeft of the Screen)

            Args:
                Diameter: the Diameter of the Circle in Pixal

                cPoint_Pos_OTopLeft: the x, y Positions of the Circle center in Pixal (Origin in the topLeft of the Screen)

            */


            circle.VerticalAlignment = VerticalAlignment.Top;
            circle.HorizontalAlignment = HorizontalAlignment.Left;

            circle.Margin = new Thickness(cPoint_Pos_OTopLeft[0] - circle.Width/2, cPoint_Pos_OTopLeft[1] - circle.Height/2, 0, 0);

            return circle;
        }

        public static float TransferTo(float value, float lower, float upper)
        {// transform value (0=<value<1) into a valueT (lower=<valueT<upper)

            float rndTime;
            rndTime = value * (upper - lower) + lower;

            return rndTime;
        }
    }
}

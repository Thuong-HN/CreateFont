using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Text;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;

namespace CreateFont {
    public partial class Form1 : Form {
        private string defaultList = " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[?]^_`abcdefghijklmnopqrstuvwxyz{|}~?ÀÁẢÃẠĂẰẮẲẴẶÂẦẤẨẪẬĐÈÉẺẼẸÊỀẾỂỄỆÌÍỈĨỊÒÓỎÕỌÔỒỐỔỖỘƠỜỚỞỠỢÙÚỦŨỤƯỪỨỬỮỰỲÝỶỸỴàáảãạăằắẳẵặâầấẩẫậđèéẻẽẹêềếểễệìíỉĩịòóỏõọôồốổỗộơờớởỡợùúủũụưừứửữựỳýỷỹỵ";
        private Thread coding;

        public Form1() {
            InitializeComponent();
            List<string> fonts = getAllFont();
            for (int i = 0; i < fonts.Count; i++) {
                fontComboBox1.Items.Add(fonts[i]);
                Font font = new Font(fonts[i], fontComboBox1.Font.Size, fontComboBox1.Font.Style, GraphicsUnit.Point, ((byte)(0)));
                fontComboBox1.Fonts.Add(font);
                font.Dispose();
            }
            fontComboBox2.Fonts.Add(new Font(fontComboBox2.Font.Name, fontComboBox2.Font.Size, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0))));
            fontComboBox2.Fonts.Add(new Font(fontComboBox2.Font.Name, fontComboBox2.Font.Size, FontStyle.Italic, GraphicsUnit.Point, ((byte)(0))));
            fontComboBox2.Fonts.Add(new Font(fontComboBox2.Font.Name, fontComboBox2.Font.Size, FontStyle.Bold, GraphicsUnit.Point, ((byte)(0))));
            fontComboBox2.Fonts.Add(new Font(fontComboBox2.Font.Name, fontComboBox2.Font.Size, FontStyle.Bold | FontStyle.Italic, GraphicsUnit.Point, ((byte)(0))));

            fontComboBox1.Text = "Times New Roman";
            fontComboBox2.SelectedIndex = 0;
            comboBox1.SelectedIndex = 2;
            textBox1.Text = fontComboBox1.Text;
        }

        private List<string> getAllFont() {
            List<string> fonts = new List<string>();
            foreach (FontFamily font in FontFamily.Families) {
                fonts.Add(font.Name);
            }
            return fonts;
        }

        private Font getFont() {
            FontStyle[] styles = {
                FontStyle.Regular,
                FontStyle.Italic,
                FontStyle.Bold,
                FontStyle.Bold | FontStyle.Italic,
            };
            Font font = new Font(fontComboBox1.Text, Convert.ToInt32(comboBox1.Text), styles[fontComboBox2.SelectedIndex], GraphicsUnit.Point, ((byte)(0)));
            return font;
        }

        private string createCode(string name, string list, Font font) {
            int yOffset0 = 0;
            int height = 0;
            createData("A", font, ref yOffset0, ref height);
            string fontMapstr = "";
            string fontstr = "";
            int index = 0;
            for (int i = 0; i < list.Length; i++) {
                if (list[i] == ' ') {
                    int w = 2;
                    fontMapstr += "  0x00, 0x00, \r\n";
                    fontstr += "  &" + name + "_map" + "[" + index + "], " + w + ", 1, 0," + "\r\n";
                    index += w;
                }
                else {
                    int yOffset = 0;
                    byte[,] data = createData(list[i] + "", font, ref yOffset, ref height);
                    yOffset = yOffset - yOffset0;
                    for (int y = 0; y < data.GetLength(1); y++) {
                        fontMapstr += "  ";
                        for (int x = 0; x < data.GetLength(0); x++) 
                            fontMapstr += "0x" + ToHex(data[x, y]) + ", ";
                        if(y == 0)
                            fontMapstr += "// " + list[i] + "\r\n";
                        else
                            fontMapstr += "\r\n";
                    }
                    fontstr += "  &" + name + "_map" + "[" + index + "], " + data.GetLength(0) + ", " + height + ", " + yOffset + ", //" + list[i] + "\r\n";
                    index += data.GetLength(0) * data.GetLength(1);
                }
            }

            fontMapstr = "static const unsigned char " + name + "_map" + "[] = {\r\n" + fontMapstr + "};";
            fontstr = "const Font " + name + "[] = {\r\n" + fontstr + "};";
            return fontMapstr + "\r\n\r\n" + fontstr;
        }

        private Bitmap drawString(string str, Font font, Rectangle rectangle) {
            if (rectangle.Width <= 0 || rectangle.Height <= 0)
                return null;
            Bitmap bitmap = new Bitmap(rectangle.Width, rectangle.Height);
            Graphics graphics = Graphics.FromImage(bitmap);

            SolidBrush solidBrush = new SolidBrush(Color.Black);
            graphics.Clear(Color.FromArgb(255, 255, 255, 255));
            graphics.TextRenderingHint = TextRenderingHint.SingleBitPerPixelGridFit;
            graphics.DrawString(str, font, solidBrush, rectangle);
            graphics.Dispose();
            return bitmap;
        }

        private string ToHex(int value) {
            string hex = "0123456789ABCDEF";
            return ((char)hex[value / 16] + "" + (char)hex[value % 16]);
        }

        private byte[,] createData(string str, Font font, ref int yOffset, ref int height) {
            Bitmap bitmap = new Bitmap(200, 200);
            Graphics graphics = Graphics.FromImage(bitmap);

            SolidBrush solidBrush = new SolidBrush(Color.Black);
            graphics.Clear(Color.FromArgb(255, 255, 255, 255));
            graphics.TextRenderingHint = TextRenderingHint.SingleBitPerPixelGridFit;
            SizeF size = graphics.MeasureString(str, font);
            Size s = new Size((int)size.Width, (int)size.Height);
            graphics.DrawString(str, font, solidBrush, new PointF(0, 0));

            int x_start = 0, y_start = 0;
            for (int x = 0; x < (s.Width + 1); x++) {
                for (int y = 0; y < (s.Height + 1); y++) {
                    if (bitmap.GetPixel(x, y) != Color.FromArgb(255, 255, 255, 255)) {
                        x_start = x + 1;
                        break;
                    }
                }
                if (x_start > 0)
                    break;
            }
            for (int y = 0; y < (s.Height + 1); y++) {
                for (int x = 0; x < (s.Width + 1); x++) {
                    if (bitmap.GetPixel(x, y).R < 128) {
                        y_start = y + 1;
                        break;
                    }
                }
                if (y_start > 0)
                    break;
            }
            x_start--;
            y_start--;

            int x_end = 0, y_end = 0;
            for (int x = s.Width; x >= 0; x--) {
                for (int y = s.Height; y >= 0; y--) {
                    if (bitmap.GetPixel(x, y).R < 128) {
                        x_end = x + 1;
                        break;
                    }
                }
                if (x_end > 0)
                    break;
            }
            for (int y = s.Height; y >= 0; y--) {
                for (int x = s.Width; x >= 0; x--) {
                    if (bitmap.GetPixel(x, y).R < 128) {
                        y_end = y + 1;
                        break;
                    }
                }
                if (y_end > 0)
                    break;
            }
            graphics.Dispose();

            int line = (y_end - y_start) / 8;
            if (((y_end - y_start) % 8) > 0)
                line++;

            byte[,] res = new byte[x_end - x_start, line];
            for(int x = x_start; x < x_end; x++) {
                for (int y = y_start; y < y_end; y++) {
                    if (bitmap.GetPixel(x, y).R < 128)
                        res[x - x_start, (y - y_start) / 8] |= (byte)(0x80 >> ((y - y_start) % 8));
                }
            }
            bitmap.Dispose();
            yOffset = y_start;
            height = y_end - y_start;
            return res;
        }

        private void fontComboBox1_SelectedIndexChanged(object sender, EventArgs e) {
            if (fontComboBox1.SelectedIndex >= 0 && fontComboBox2.SelectedIndex >= 0 && comboBox1.SelectedIndex >= 0) {
                Font font = getFont();
                pictureBox1.Image = drawString("Chào bạn! Đây là mẫu font được tạo nên từ các lựa chọn cấu hình của bạn.", font, new Rectangle(0, 0, pictureBox1.Width, pictureBox1.Height));
                panel3.Visible = true;
                if (coding != null && coding.IsAlive)
                    coding.Abort();
                string arr_name = textBox1.Text;
                string list = textBox2.Text;
                list = Regex.Replace(list, "(\r|\n)", "");
                if (list == "") {
                    list = defaultList;
                    textBox2.Text = " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQ\r\nRSTUVWXYZ[ ]^_`abcdefghijklmnopqrstuvwxyz{|}~ ÀÁẢÃẠ\r\nĂẰẮẲẴẶÂẦẤẨẪẬĐÈÉẺẼẸÊỀẾỂỄỆÌÍỈĨỊÒÓỎÕỌÔỒỐỔỖỘƠ\r\nỜỚỞỠỢÙÚỦŨỤƯỪỨỬỮỰỲÝỶỸỴàáảãạăằắẳẵặâầấẩẫậđ\r\nèéẻẽẹêềếểễệìíỉĩịòóỏõọôồốổỗộơờớởỡợùúủũụưừứửữựỳýỷỹỵ";
                }

                coding = new Thread(delegate () {
                    string str = createCode(arr_name, list, font);
                    BeginInvoke((MethodInvoker)delegate () {
                        richTextBox1.Text = str;
                        panel3.Visible = false;
                    });
                });
                coding.IsBackground = true;
                coding.Start();
            }
        }

        private void pictureBox1_SizeChanged(object sender, EventArgs e) {
            if (fontComboBox1.SelectedIndex >= 0 && fontComboBox2.SelectedIndex >= 0 && comboBox1.SelectedIndex >= 0)
                pictureBox1.Image = drawString("Chào bạn! Đây là mẫu font được tạo nên từ các lựa chọn cấu hình của bạn.", getFont(), new Rectangle(0, 0, pictureBox1.Width, pictureBox1.Height));
        }

        private void textBox1_TextChanged(object sender, EventArgs e) {
            string str = textBox1.Text.Replace(" ", "_");
            textBox1.Text = str;
            label3.Text = "const Font " + str + "[];";
        }

        private void fontComboBox1_SelectedIndexChanged_1(object sender, EventArgs e) {
            textBox1.Text = fontComboBox1.Text;
            fontComboBox1_SelectedIndexChanged(sender, e);
        }
    }
}

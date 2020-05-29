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
using System.IO;

namespace CreateFont {
    public partial class Form1 : Form {
        private string defaultList = " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[?]^_`abcdefghijklmnopqrstuvwxyz{|}~?ÀÁẢÃẠĂẰẮẲẴẶÂẦẤẨẪẬĐÈÉẺẼẸÊỀẾỂỄỆÌÍỈĨỊÒÓỎÕỌÔỒỐỔỖỘƠỜỚỞỠỢÙÚỦŨỤƯỪỨỬỮỰỲÝỶỸỴàáảãạăằắẳẵặâầấẩẫậđèéẻẽẹêềếểễệìíỉĩịòóỏõọôồốổỗộơờớởỡợùúủũụưừứửữựỳýỷỹỵ";
        private Thread coding;

        public Form1() {
            InitializeComponent();
            int indexDefault = 0;
            for (int i = 0; i < FontFamily.Families.Length; i++) {
                fontComboBox1.Items.Add(FontFamily.Families[i].Name);
                FontStyle fontStyle = fontComboBox1.Font.Style;
                if (!FontFamily.Families[i].IsStyleAvailable(fontComboBox1.Font.Style)){
                    if (FontFamily.Families[i].IsStyleAvailable(FontStyle.Regular))
                        fontStyle = FontStyle.Regular;
                    else if (FontFamily.Families[i].IsStyleAvailable(FontStyle.Italic))
                        fontStyle = FontStyle.Italic;
                    else if (FontFamily.Families[i].IsStyleAvailable(FontStyle.Bold))
                        fontStyle = FontStyle.Bold;
                    else if (FontFamily.Families[i].IsStyleAvailable(FontStyle.Bold | FontStyle.Italic))
                        fontStyle = FontStyle.Bold | FontStyle.Italic;
                }
                Font font = new Font(FontFamily.Families[i].Name, fontComboBox1.Font.Size, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)));
                fontComboBox1.Fonts.Add(font);
                font.Dispose();
                if (FontFamily.Families[i].Name.CompareTo("Times New Roman") == 0)
                    indexDefault = i;
            }
            fontComboBox1.SelectedIndex = indexDefault;
            updateFontStyleList();

            fontComboBox2.SelectedIndex = 0;
            comboBox1.SelectedIndex = 2;
            textBox1.Text = fontComboBox1.Text;
        }

        private void updateFontStyleList() {
            fontComboBox2.SelectedIndex = fontComboBox2.SelectedIndex;
            string str = fontComboBox2.Text;
            int index = 0;
            List<FontStyle> fontStyles = getFontStyleList(FontFamily.Families[fontComboBox1.SelectedIndex]);
            fontComboBox2.Items.Clear();
            for (int i = 0; i < fontStyles.Count; i++) {
                fontComboBox2.Items.Add(fontStyles[i]);
                fontComboBox2.Fonts.Add(new Font(FontFamily.Families[fontComboBox1.SelectedIndex].Name, fontComboBox2.Font.Size, fontStyles[i], GraphicsUnit.Point, ((byte)(0))));
                if (fontStyles[i].ToString().CompareTo(str) == 0)
                    index = i;
            }
            fontComboBox2.SelectedIndex = index;
        }

        private List<FontStyle> getFontStyleList(FontFamily fontFamily) {
            List<FontStyle> fontStyles = new List<FontStyle>();
            if (fontFamily.IsStyleAvailable(FontStyle.Regular))
                fontStyles.Add(FontStyle.Regular);
            if (fontFamily.IsStyleAvailable(FontStyle.Italic))
                fontStyles.Add(FontStyle.Italic);
            if (fontFamily.IsStyleAvailable(FontStyle.Bold))
                fontStyles.Add(FontStyle.Bold);
            if (fontFamily.IsStyleAvailable(FontStyle.Bold | FontStyle.Italic))
                fontStyles.Add(FontStyle.Bold | FontStyle.Italic);
            return fontStyles;
        }

        private Font getFont() {
            List<FontStyle> styles = getFontStyleList(FontFamily.Families[fontComboBox1.SelectedIndex]);
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

                    Bitmap bitmap = new Bitmap(200, 200);
                    Graphics graphics = Graphics.FromImage(bitmap);
                    int space_w = (int)graphics.MeasureString(" ", font).Width;
                    graphics.Dispose();
                    bitmap.Dispose();

                    fontMapstr += "  ";
                    for (int s = 0; s < space_w; s++)
                        fontMapstr += "0x00, ";
                    fontMapstr += "\r\n";
                    fontstr += "  &" + name + "_map" + "[" + index + "], " + space_w + ", 1, 0," + "\r\n";
                    index += space_w;
                }
                else {
                    int yOffset = 0;
                    byte[,] data = null;
                    try {
                        data = createData(list[i] + "", font, ref yOffset, ref height);
                    }
                    catch {
                        data = createData("?", font, ref yOffset, ref height);
                    }
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
                    fontstr += "  &" + name + "_map" + "[" + index + "], " + data.GetLength(0) + ", " + height + ", " + yOffset + ", // " + list[i] + "\r\n";
                    index += data.GetLength(0) * data.GetLength(1);
                }
            }

            fontMapstr = "static const unsigned char " + name + "_map" + "[] = {\r\n" + fontMapstr + "};";
            fontstr = "const Font " + name + "[] = {\r\n" + fontstr + "};";
            return "/*\r\ntypedef struct {\r\n  const unsigned char* map;\r\n  unsigned char W;\r\n  unsigned char H;\r\n  signed char yOffset;\r\n} Font;\r\n*/\r\n\r\n" + fontMapstr + "\r\n\r\n" + fontstr;
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
                pictureBox1.Image = drawString("!\"#$%&'()*+,-. 0123456789 ABCDEF abcdef\r\nChào Bạn\r\nこんにちは\r\n你好\r\n여보세요\r\nПривет\r\nहैलो\r\nสวัสดี\r\nសួស្តី", font, new Rectangle(0, 0, pictureBox1.Width, pictureBox1.Height));
                panel3.Visible = true;
                if (coding != null && coding.IsAlive)
                    coding.Abort();
                string arr_name = textBox1.Text.Replace(" ", "_");
                if (arr_name == "")
                    arr_name = fontComboBox1.Text.Replace(" ", "_");

                string list = textBox2.Text;
                list = Regex.Replace(list, "(\r|\n)", "");
                if (list == "") 
                    list = defaultList;

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
                pictureBox1.Image = drawString("!\"#$%&'()*+,-. 0123456789 ABCDEF abcdef\r\nChào Bạn\r\nこんにちは\r\n你好\r\n여보세요\r\nПривет\r\nहैलो\r\nสวัสดี\r\nសួស្តី", getFont(), new Rectangle(0, 0, pictureBox1.Width, pictureBox1.Height));
        }

        private void textBox1_TextChanged(object sender, EventArgs e) {
            int index = textBox1.SelectionStart;
            string str = textBox1.Text.Replace(" ", "_");
            while (str != "" && int.TryParse(str[0] + "", out _))
                str = str.Substring(1);
            textBox1.Text = str;
            textBox1.SelectionStart = index;
            if (str == "")
                str = fontComboBox1.Text.Replace(" ", "_");
            label3.Text = "const Font " + str + "[];";
            fontComboBox1_SelectedIndexChanged(sender, e);
        }

        private void fontComboBox1_SelectedIndexChanged_1(object sender, EventArgs e) {
            textBox1.Text = fontComboBox1.Text;
            updateFontStyleList();
            fontComboBox1_SelectedIndexChanged(sender, e);
        }

        private void about_menu_Click(object sender, EventArgs e) {
            About about = new About();
            about.ShowDialog();
            about.Dispose();
        }

        private void exit_menu_Click(object sender, EventArgs e) {
            Confirm confirm = new Confirm("Are you sure you want to exit?");
            confirm.ShowDialog();
            if (confirm.reply)
                Application.Exit();
            confirm.Dispose();
        }

        private void create_c_Click(object sender, EventArgs e) {
            if (coding != null && !coding.IsAlive) {
                FolderSelectDialog folderSelectDialog = new FolderSelectDialog();
                folderSelectDialog.Title = "Select Folder";
                if (folderSelectDialog.ShowDialog() == true) {
                    string folder = folderSelectDialog.FileName;
                    StreamWriter streamWriter = new StreamWriter(folder + "\\" + textBox1.Text + ".c");
                    string code = richTextBox1.Text.Substring(richTextBox1.Text.IndexOf("*/") + 4);
                    streamWriter.Write("\r\n#include \"" + textBox1.Text + ".h\r\n\r\n" + code + "\r\n");
                    streamWriter.Close();

                    streamWriter = new StreamWriter(folder + "\\" + textBox1.Text + ".h");
                    streamWriter.WriteLine("");
                    streamWriter.WriteLine("#ifndef __" + textBox1.Text);
                    streamWriter.WriteLine("#define __" + textBox1.Text);
                    streamWriter.WriteLine("");
                    streamWriter.Write("typedef struct {\r\n  const unsigned char* map;\r\n  unsigned char W;\r\n  unsigned char H;\r\n  signed char yOffset;\r\n} Font;\r\n");
                    streamWriter.WriteLine("");
                    streamWriter.WriteLine("extern const Font " + textBox1.Text + "[];");
                    streamWriter.WriteLine("");
                    streamWriter.WriteLine("#endif");
                    streamWriter.Close();

                    Notification notification = new Notification("Created:\r\n" + textBox1.Text + ".h\r\n" + textBox1.Text + ".c\r\n");
                    notification.ShowDialog();
                    notification.Dispose();
                }
            }
            else {
                Notification notification = new Notification("The process is busy!");
                notification.ShowDialog();
                notification.Dispose();
            }
        }
    }
}

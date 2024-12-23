using DevExpress.Xpf.Core;
using HalconDotNet;
using NOVisionDesigner.Designer.Misc;
using NOVisionDesigner.Designer.Nodes;
using NOVisionDesigner.Designer.Windows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace NOVisionDesigner.Designer.PropertiesViews
{
    /// <summary>
    /// Interaction logic for ColorDetectionView.xaml
    /// </summary>
    public partial class ColorDetectionView : UserControl
    {
        public ColorDetectionView(ColorDetectionNode node)
        {
            this.colorDetectionNode = node;
            InitializeComponent();
            this.DataContext = colorDetectionNode.ColorDetection;
        }
        public ColorDetectionNode colorDetectionNode;
        private void btn_draw_check_region_mask_Click(object sender, RoutedEventArgs e)
        {
            //try
            //{

            //    if (colorDetectionNode.ImageInput.Value!= null)
            //    {

            //        MaskEditorWindow draw;
            //        if (colorDetectionNode.FixtureInput.Value != null)
            //            draw = new MaskEditorWindow(colorDetectionNode.ImageInput.Value.CopyImage(), colorDetectionNode.FixtureInput.Value.Clone(), colorDetectionNode.ColorDetection);
            //        else
            //            draw = new MaskEditorWindow(colorDetectionNode.ImageInput.Value.CopyImage(), new HHomMat2D(), colorDetectionNode.ColorDetection);
            //        draw.Show();
            //    }
            //}
            //catch (Exception ex)
            //{
            //    MessageBox.Show(ex.Message);
            //}
        }

        private void btn_import_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.OpenFileDialog op = new System.Windows.Forms.OpenFileDialog();
            if (op.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                try
                {
                    colorDetectionNode.ColorDetection.Load(new DeserializeFactory(new HFile(op.FileName, "input_binary"), new HSerializedItem(), op.FileName));
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        private void btn_view_dataset_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (colorDetectionNode.ImageInput.Value == null)
                {
                    MessageBox.Show("Select Image First");
                    return;
                }
                if (colorDetectionNode.ImageInput.Value.CountObj() > 0)
                {
                    try
                    {
                        HTuple channels = colorDetectionNode.ImageInput.Value.CountChannels();
                        if (channels < 3)
                        {
                            DXMessageBox.Show("Color detection require colored image (3 channel) to run. Please check input image!!");
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        DXMessageBox.Show("Error checking image channels");
                        return;
                    }
                    TrainColorMlpWindow wd = new TrainColorMlpWindow(colorDetectionNode.ColorDetection.ClassMLP, colorDetectionNode.ColorDetection.ClassLUTColor, colorDetectionNode.ImageInput.Value.CopyImage(), colorDetectionNode.ColorDetection);
                    wd.ShowDialog();
                }
                else
                {
                    MessageBox.Show("No Image in in Window");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void btn_draw_check_region_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (colorDetectionNode.ImageInput.Value != null)
                {

                    WindowRegionWindowInteractive draw;
                    if (colorDetectionNode.FixtureInput.Value != null)
                        draw = new WindowRegionWindowInteractive(colorDetectionNode.ImageInput.Value.CopyImage(), colorDetectionNode.ColorDetection.Region, colorDetectionNode.FixtureInput.Value.Clone());
                    else
                        draw = new WindowRegionWindowInteractive(colorDetectionNode.ImageInput.Value.CopyImage(), colorDetectionNode.ColorDetection.Region, new HHomMat2D());
                    draw.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void btn_draw_check_region1_Click(object sender, RoutedEventArgs e)
        {
            try
            {

                if (colorDetectionNode.ImageInput.Value != null)
                {

                    DrawRegionWindow draw;
                    if (colorDetectionNode.FixtureInput.Value != null)
                        draw = new DrawRegionWindow(colorDetectionNode.ImageInput.Value.CopyImage(), colorDetectionNode.ColorDetection.Region, colorDetectionNode.FixtureInput.Value.Clone());
                    else
                        draw = new DrawRegionWindow(colorDetectionNode.ImageInput.Value.CopyImage(), colorDetectionNode.ColorDetection.Region, new HHomMat2D());
                    draw.Show();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            //try
            //{

            //    if (stains_detection.InputBlock.Target1.LastData != null)
            //    {

            //        MaskEditorWindow draw;
            //        if (stains_detection.InputBlock.Target2.LastData != null)
            //            draw = new MaskEditorWindow(stains_detection.InputBlock.Target1.LastData.CopyImage(), stains_detection.InputBlock.Target2.LastData.Clone(),stains_detection.Region);
            //        else
            //            draw = new MaskEditorWindow(stains_detection.InputBlock.Target1.LastData.CopyImage(), new HHomMat2D(),stains_detection.Region);
            //        draw.Show();
            //    }
            //}
            //catch (Exception ex)
            //{
            //    MessageBox.Show(ex.Message);
            //}
        }

        private void btn_edit_rejection_classifier_Click(object sender, RoutedEventArgs e)
        {
            EditRejectionClassWindow wd = new EditRejectionClassWindow(colorDetectionNode.ColorDetection.LstRejectionClass);
            colorDetectionNode.ColorDetection.RefreshRejectionClass();
            wd.ShowDialog();
        }

        private void btn_export_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.SaveFileDialog sav = new System.Windows.Forms.SaveFileDialog();
            if (sav.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                try
                {
                    colorDetectionNode.ColorDetection.Save(new HFile(sav.FileName, "output_binary"));
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }
    }
}

﻿/*
    ConfigGCPV - Outil d'aide à la configuration de fichier PAT
    Copyright (C) 2022  François-Xavier Choinière

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <https://www.gnu.org/licenses/>.

    François-Xavier Choinière, fx@efficks.com
*/
using GCPVConfig.Minute;
using Microsoft.Win32;
using Octokit;
using QuestPDF.Infrastructure;
using Semver;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms.Design;
using System.Windows.Input;

namespace GCPVConfig
{
    public class CourseTemps
    {
        private string distance = "";
        private string secondes = "";

        public string Distance { get => distance; set => distance = value; }
        public string Secondes { get => secondes; set => secondes = value; }
    }

    public class ClassementComboItem
    {
        private string text = "";
        private string mValue = "";

        public string Text { get => text; set => text = value; }
        public string Value { get => mValue; set => mValue = value; }

        public override string ToString()
        {
            return Text;
        }
    }

    public class CompetitionComboItem
    {
        private string text;

        public string Text { get => text; set => text = value; }
        public FichierPAT.Competition Value { get; set; }

        public CompetitionComboItem(FichierPAT.Competition value)
        {
            text = value.Nom;
            Value = value;
        }

        public override string ToString()
        {
            return Value.Nom;
        }
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private delegate void NoArgDelegate(IProgress<string> progress);
        private ObservableCollection<CourseTemps> EmployeeCollection = new ObservableCollection<CourseTemps>();
        private Config mConfig;
        System.Windows.Threading.DispatcherTimer dispatcherTimer;

        public MainWindow()
        {
            QuestPDF.Settings.License = LicenseType.Community;
            InitializeComponent();
            ValidImportation();
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            this.Title = String.Format("GCPV Config version {0}", Common.version());

            try
            {
                string strExeFilePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                string? strWorkPath = System.IO.Path.GetDirectoryName(strExeFilePath);
                if(strWorkPath is null)
                {
                    MessageBox.Show("Erreur d'ouverture du fichier de configuration. Impossible d'utiliser GetDirectoryName",
                        "Erreur de configuration",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    throw new Exception();
                }

                string configPath = Path.Combine(strWorkPath, "GCPVConfig.yaml");

                mConfig = Config.load(configPath);
            }
            catch(Exception e)
            {
                MessageBox.Show("Erreur lors de l'ouverture du fichier de configuration GCPVConfig.yaml. Exception: "+e.ToString(),
                    "Erreur de configuration",
                    MessageBoxButton.OK,MessageBoxImage.Error);
                throw;
            }

            foreach (var typecompe in mConfig.TypeCompetition)
            {
                this.combo_evenement_type.Items.Add(typecompe.Name);
            }

            dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
            dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 3);
            dispatcherTimer.Start();

        }
        private async void dispatcherTimer_Tick(object? sender, EventArgs e)
        {
            dispatcherTimer.Stop();
            try
            {
                var client = new GitHubClient(new ProductHeaderValue("efficks_GCPVConfig"));
                var latest = await client.Repository.Release.GetLatest("efficks", "GCPVConfig");
                if (latest is not null)
                {
                    var currentVersion = SemVersion.Parse(Common.version(), SemVersionStyles.Any);
                    var gitVersion = SemVersion.Parse(latest.Name, SemVersionStyles.Any);
                    if (currentVersion.ComparePrecedenceTo(gitVersion) == -1)
                    {
                        var w = new UpdateWindow(latest.HtmlUrl);
                        w.Owner = Window.GetWindow(this);
                        w.ShowDialog();
                    }
                }
            }
            catch(Exception)
            {
                // Ignore exception on update
            }
        }

        /*private void DG1_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            string? headername = e.Column.Header.ToString();

            if(headername is null)
            {

            }

            //Cancel the column you don't want to generate
            if (headername == "MiddleName")
            {
                e.Cancel = true;
            }

            //update column details when generating
            if (headername == "FirstName")
            {
                e.Column.Header = "First Name";
            }
            else if (headername == "LastName")
            {
                e.Column.Header = "Last Name";
            }
            else if (headername == "EmailAddress")
            {
                e.Column.Header = "Email";
            }
        }*/

        private void btn_openInscription_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Fichier d'inscription (*.xlsx;*.csv)|*.xlsx;*.csv|Tous les fichiers (*.*)|*.*";
            if (openFileDialog.ShowDialog() == true)
            {
                txt_inscriptionPath.Text = openFileDialog.FileName;
            }
            ValidImportation();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Fichier PAT (*.pat)|*.pat|Tous les fichiers (*.*)|*.*";
            if (openFileDialog.ShowDialog() == true)
            {
                var old_cursor = this.Cursor;
                try
                {
                    this.Cursor = Cursors.Wait;
                    txt_patPath.Text = openFileDialog.FileName;

                    combo_competition_list.Items.Clear();
                    combo_classement.Items.Clear();

                    if (File.Exists(txt_patPath.Text))
                    {
                        var patfile = FichierPAT.Open(txt_patPath.Text);
                        var competitions = patfile.GetCompetitions();

                        foreach (var compe in competitions)
                        {
                            combo_competition_list.Items.Add(new CompetitionComboItem(compe)
                            {
                                Text = compe.Nom,
                                Value = compe
                            });
                        }

                        var classements = patfile.GetClassementName();
                        foreach (var c in classements)
                        {
                            ClassementComboItem newitem = new ClassementComboItem();
                            newitem.Value = c.Item1;
                            newitem.Text = c.Item2;

                            combo_classement.Items.Add(newitem);
                        }

                        combo_competition_list.IsEnabled = true;
                        combo_classement.IsEnabled = true;
                    }
                    else
                    {
                        combo_competition_list.IsEnabled = false;
                        combo_classement.IsEnabled = false;
                    }

                }
                finally
                {
                    this.Cursor = old_cursor;
                }
            }
        }

        private void ValidImportation()
        {
            btn_launchImport.IsEnabled = txt_patPath.Text.Length > 0 && txt_inscriptionPath.Text.Length > 0 && combo_evenement_type.SelectedValue != null && combo_competition_list.SelectedValue != null;

            btn_launchRegroupement.IsEnabled = txt_patPath.Text.Length > 0 && combo_evenement_type.SelectedValue != null && combo_competition_list.SelectedValue != null && combo_classement.SelectedValue != null;

            if (btn_launchMinute != null)
            {
                btn_launchMinute.IsEnabled = txt_patPath.Text.Length > 0 && combo_evenement_type.SelectedValue != null && combo_competition_list.SelectedValue != null;
            }
        }

        private void txt_patPath_TextChanged(object sender, TextChangedEventArgs e)
        {
            ValidImportation();
        }

        private void txt_inscriptionPath_TextChanged(object sender, TextChangedEventArgs e)
        {
            ValidImportation();
        }

        private async void btn_launchImport_Click(object sender, RoutedEventArgs e)
        {
            btn_launchImport.IsEnabled = false;
            var old_cursor = Cursor;
            Cursor = Cursors.Wait;
            Progress<string> progressMessage = new Progress<string>(msg => NewMessageReport(msg));

            if(combo_evenement_type.SelectedItem is null)
            {
                NewMessageReport("Aucun type d'événement sélectionné");
                return;
            }

            txt_log.Clear();
            try
            {

                string inscriptionPath = txt_inscriptionPath.Text;
                string patPath = txt_patPath.Text;
                Importer importer = new Importer(inscriptionPath, patPath, mConfig, (string)combo_evenement_type.SelectedItem);
                importer.ProgressMessage = progressMessage;
                importer.ConflictFound += ConflictFound;
                importer.SelectCompetition += SelectCompetition;
                importer.SelectClub += SelectClub;
                await importer.Import();
            }
            catch(Exception exception)
            {
                NewMessageReport("Une erreur est survenue durant l'importation.");
                NewMessageReport(exception.ToString());
            }
            finally
            {
                btn_launchImport.IsEnabled = true;
                Cursor = old_cursor;
            }
        }
        private void NewMessageReport(string msg)
        {
            lock (txt_log)
            {
                txt_log.AppendText(msg + System.Environment.NewLine);
                txt_log.ScrollToEnd();
            }
        }

        private void SelectCompetition(Object sender, SelectCompetitionEventArgs e)
        {
            this.Dispatcher.Invoke(() =>
            {
                try
                {
                    e.Choice = ((CompetitionComboItem)combo_competition_list.SelectedValue).Value;
                }
                finally
                {
                    e.MRE.Set();
                }
            });
        }
        private void SelectClub(Object sender, SelectClubEventArgs e)
        {
            this.Dispatcher.Invoke(() =>
            {
                try
                {
                    e.Choice = null;
                    GCPVConfig.SelectClub selectWindow = new SelectClub(e.Clubs,e.WrongName);
                    selectWindow.Owner = Window.GetWindow(this);

                    var result = selectWindow.ShowDialog();
                    if(result is not null && result == true)
                    {
                        e.Choice = selectWindow.Choice;
                    }
                }
                finally
                {
                    e.MRE.Set();
                }
            });
        }

        private void ConflictFound(Object sender, ConflictFoundEventArgs e)
        {
            this.Dispatcher.Invoke(() =>
            {
                try
                {
                    ConflitWindow cw = new ConflitWindow(e.Inscription, e.Patineur);
                    cw.Owner = Window.GetWindow(this);

                    var result = cw.ShowDialog();
                    if (result is not null && result == true)
                    {
                        e.UserAction = cw.Result;
                    }
                    else
                    {
                        e.UserAction = Importer.ConflictAction.Cancel;
                    }
                }
                catch
                {
                    e.UserAction = Importer.ConflictAction.Cancel;
                    throw;
                }
                finally
                {
                    e.MRE.Set();
                }
            });
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            AideDialogue ad = new AideDialogue();
            ad.ShowDialog();
        }

        private void btn_launchMinute_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveMinuteDialog = new SaveFileDialog();
            saveMinuteDialog.Filter = "Fichier PDF (*.pdf)|*.pdf|Tous les fichiers (*.*)|*.*";
            saveMinuteDialog.FileName = $"Horaire minuté - {((CompetitionComboItem)combo_competition_list.SelectedValue).Text}.pdf";
            string savePath = "";
            if (saveMinuteDialog.ShowDialog(this)==false)
            {
                return;
            }
            savePath = saveMinuteDialog.FileName;

            Progress<string> progressMessage = new Progress<string>(msg => NewMessageReport(msg));
            
            FichierPAT pat = FichierPAT.Open(txt_patPath.Text);

            Minuteur.MinuteParameters parameters = new Minuteur.MinuteParameters
            {
                VerifLame = TimeOnly.Parse(txt_time_verif_lame.Text),
                DebutRechauffements = TimeOnly.Parse(txt_warmup_time.Text),
                DureeRechauffements = TimeSpan.FromMinutes(Int32.Parse(txt_warmup_duration.Text)),
                RencontreEntraineur = TimeOnly.Parse(txt_meeting_time.Text),
                DebutCourse = TimeOnly.Parse(txt_race_time.Text),
                DureeResurfacage = TimeSpan.FromMinutes(Int32.Parse(txt_resurfacage_duration.Text)),
                DureeDiner = TimeSpan.FromMinutes(Int32.Parse(txt_diner_time.Text)),
                BlocAvantDiner = Int32.Parse(txt_blocDiner.Text),
                SecondesParTour = Int32.Parse(txt_secondes_tours.Text),
                SecondesEntreeSortie = Int32.Parse(txt_temps_in_out.Text)
            };

            Minuteur m = new Minuteur(pat, ((CompetitionComboItem)combo_competition_list.SelectedValue).Value, parameters);
            m.ProgressMessage = progressMessage;

            m.Generate(savePath);
        }

        private void combo_evenement_type_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.ValidImportation();
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {

        }

        private async void btn_launchRegroupement_Click(object sender, RoutedEventArgs e)
        {
            btn_launchRegroupement.IsEnabled = false;
            var old_cursor = Cursor;
            Cursor = Cursors.Wait;

            Progress<string> progressMessage = new Progress<string>(msg => NewMessageReport(msg));


            txt_log.Clear();
            string inscriptionPath = txt_inscriptionPath.Text;
            string patPath = txt_patPath.Text;

            if (combo_evenement_type.SelectedItem is null)
            {
                NewMessageReport("Auncun type d'événement sélectionné");
                return;
            }

            Importer importer = new Importer(inscriptionPath, patPath, mConfig, (string)combo_evenement_type.SelectedItem);
            importer.ProgressMessage = progressMessage;
            importer.ConflictFound += ConflictFound;
            importer.SelectCompetition += SelectCompetition;
            await importer.Regroup( ((ClassementComboItem)combo_classement.SelectedItem).Value);

            btn_launchRegroupement.IsEnabled = true;
            Cursor = old_cursor;
        }

        private void combo_classement_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.ValidImportation();
        }
    }
}

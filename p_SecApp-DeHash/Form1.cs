using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace p_SecApp_DeHash
{
    public partial class DeHash : Form
    {
        [DllImport("user32.dll")]
        static extern bool FlashWindow(IntPtr hwnd, bool bInvert);

        int MINLENGTH = 1;
        int MAXLENGTH = 1;
        int CurrentLength = 1;
        char[] Word = new char[1];

        StringBuilder SB = new StringBuilder("");

        Thread t = null;
        Thread t1 = null;

        int[] CharsToUse = new int[1];

        string SEARCHING = "";
        string CURRENT = "";
        string CURRENTDIC = "";

        ulong PossibleCombos = 0;
        ulong AttemptsPerSecond = 0;
        ulong TotalAttempts = 0;
        ulong SecondsToComplete = 0;
        double TimeEstimate = 0;
        bool MatchFound = false;
        bool BackThreadRunning = false;
        string MessageToShow = "";
        string Password = "";

        Utils.Utils.hashtype hashtype;
        public DeHash()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Start
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnStartResume_Click(object sender, EventArgs e)
        {
            //Limite de nombre de caractere
            if (int.TryParse(txtLowerLimit.Text, out MINLENGTH) == false ||
                int.TryParse(txtUpperLimit.Text, out MAXLENGTH) == false)
            {
                ShowErrorBox("Min and Max lengths must be valid integers.");
                return;
            }
            //Longueur max
            if (MINLENGTH > 16 || MAXLENGTH > 16)
            {
                ShowErrorBox("Min and Max lengths cannot be greater than 16.");
                return;
            }
            //Si min > max
            if (MINLENGTH > MAXLENGTH)
            {
                ShowErrorBox("Min length cannot be greater than Max length.");
                return;
            }
            //Si il n y a pas de class de caractere
            if (chkLower.Checked == false && chkUpper.Checked == false &&
                chkNumeric.Checked == false && chkSpecial.Checked == false)
            {
                ShowErrorBox("You must select which characters set to use.");
                return;
            }

            //Get hash Type
            hashtype = Utils.Utils.GetType(txtHash.Text);

            //Si on a trouvé le cryptage
            if (hashtype != Utils.Utils.hashtype.unknown)
            {
                //On met en attente
                LockForm();
                //On crée l array de tous les caractère
                CreateCharacterArray();

                SEARCHING = "";

                BackThreadRunning = true;
                MatchFound = false;

                string mashup = txtHash.Text.Trim().ToUpper();

                //Séparation en groupe 
                for (int i = 0; i < mashup.Length; i += 2)
                {
                    SEARCHING += mashup.Substring(i, 2);
                    SEARCHING += "-";
                }
                SEARCHING = SEARCHING.Substring(0, SEARCHING.Length - 1);


                t = new Thread(BruteForceProcess) { Name = "BruteForce" };
                t1 = new Thread(BruteForceDictonnary) { Name = "Dictionnary" };

                t1.Start();
                t.Start();  
            }
            else
            {
                ShowErrorBox("No hash type found");
                UnlockForm();
            }
        }

        /// <summary>
        /// BruteForceProcess
        /// </summary>
        private void BruteForceProcess()
        {
            DateTime startTime = DateTime.Now;
            try
            {
                SB = new StringBuilder();

                TotalAttempts = 0;
                PossibleCombos = 0;
                for (int i = 1; i <= MAXLENGTH; i++)
                {
                    PossibleCombos += (ulong)Math.Pow((double)CharsToUse.Length, (double)i);
                }
                CreateCharArray(MINLENGTH);

                CurrentLength = MINLENGTH;
                for (int outerCount = MINLENGTH; outerCount <= MAXLENGTH; outerCount++)
                {
                    CycleChar(0);

                    if (BackThreadRunning == false)
                        break;

                    //Increase word length
                    CurrentLength++;
                    CreateCharArray(CurrentLength);
                }
            }
            catch (Exception ex)
            {
                BackThreadRunning = false;
                MessageToShow = "Error!\n" + ex.Message;
            }


            DateTime endTime = DateTime.Now;
            TimeSpan ts = endTime - startTime;
            int secondsTaken = ts.Seconds;

            //Si on a pas trouvé de résultat
            if (BackThreadRunning == true)
                MessageToShow = "No matching "+ hashtype.ToString() +" was found. Seconds Taken: " + secondsTaken.ToString();
            else if (BackThreadRunning == false && MatchFound == true)
                MessageToShow = "Matching " + hashtype.ToString() + " found! Seconds Taken: " + secondsTaken.ToString() + ". Your password is : ";

            BackThreadRunning = false;
        }

        /// <summary>
        /// BruteForceDictonnary
        /// </summary>
        /// <param name="numDic"></param>
        private void BruteForceDictonnary()
        {
            string[] lines = File.ReadAllLines(Directory.GetCurrentDirectory() + $"\\Dictonnary\\dic.txt");

            Utils.hash hash = new Utils.hash();

            foreach (string line in lines)
            {
                switch (hashtype)
                {
                    case Utils.Utils.hashtype.md5:
                        CURRENTDIC = hash.EncodeMD5(line);
                        break;
                    case Utils.Utils.hashtype.sha2:
                        CURRENTDIC = hash.EncodeSHA256(line);
                        break;
                    case Utils.Utils.hashtype.sha1:
                        CURRENTDIC = hash.EncodeSHA1(line);
                        break;
                }

                //Si on a le résultat
                if (CURRENTDIC == SEARCHING)
                {
                    Password = line;
                    MatchFound = true;
                    BackThreadRunning = false;
                    return;
                }
            }   
        }

        /// <summary>
        /// CycleChar
        /// </summary>
        /// <param name="position"></param>
        private void CycleChar(int position)
        {
            if (BackThreadRunning == false)
                return;

            //End characters must be cycled each time a more starting character is cycled.
            for (int b = 0; b < CharsToUse.Length; b++)
            {
                Word[position] = (char)CharsToUse[b];
                if (position < (Word.Length - 1))
                {
                    CycleChar(position + 1);
                }
                else
                {
                    UseWord();
                    if (MatchFound == true)
                    {
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Affichage du mot utiliser
        /// </summary>
        private void UseWord()
        {
            Utils.hash hash = new Utils.hash();

            //Write this word
            SB.Append(Word);
            switch (hashtype)
            {
                case Utils.Utils.hashtype.md5:
                    CURRENT = hash.EncodeMD5(SB.ToString());
                    break;
                case Utils.Utils.hashtype.sha2:
                    CURRENT = hash.EncodeSHA256(SB.ToString());
                    break;
                case Utils.Utils.hashtype.sha1:
                    CURRENT = hash.EncodeSHA1(SB.ToString());
                    break;
            }
            

            AttemptsPerSecond++;
            TotalAttempts++;

            //Si on a le résultat
            if (CURRENT == SEARCHING)
            {
                Password = SB.ToString();
                MatchFound = true;
                BackThreadRunning = false;
                return;
            }

            SB = new StringBuilder();
        }

        /// <summary>
        /// Création de l array avec tout les cracteres séléctionner
        /// </summary>
        /// <param name="size"></param>
        private void CreateCharArray(int size)
        {
            Word = new char[size];
            for (int i = 0; i < Word.Length; i++)
            {
                Word[i] = (char)CharsToUse[0];
            }
        }

        /// <summary>
        /// Methode CreateCharacterArray
        /// </summary>
        private void CreateCharacterArray()
        {
            int NumberOfChars = 0;
            int counter = 0;

            if (chkLower.Checked == true)
            {
                NumberOfChars += Program.Lowercase.Length;
            }
            if (chkUpper.Checked == true)
            {
                NumberOfChars += Program.Uppercase.Length;
            }
            if (chkNumeric.Checked == true)
            {
                NumberOfChars += Program.Numeric.Length;
            }
            if (chkSpecial.Checked == true)
            {
                NumberOfChars += Program.Special.Length;
            }

            CharsToUse = new int[NumberOfChars];

            if (chkLower.Checked == true)
            {
                foreach (int cha in Program.Lowercase)
                {
                    CharsToUse[counter] = cha;
                    counter++;
                }
            }
            if (chkUpper.Checked == true)
            {
                foreach (int cha in Program.Uppercase)
                {
                    CharsToUse[counter] = cha;
                    counter++;
                }
            }
            if (chkNumeric.Checked == true)
            {
                foreach (int cha in Program.Numeric)
                {
                    CharsToUse[counter] = cha;
                    counter++;
                }
            }
            if (chkSpecial.Checked == true)
            {
                foreach (int cha in Program.Special)
                {
                    CharsToUse[counter] = cha;
                    counter++;
                }
            }
        }

        /// <summary>
        /// Excution tick
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tmrMain_Tick(object sender, EventArgs e)
        {
            lblStatus.Visible = true;

            try
            {
                txtResume.Text = SB.ToString();
                //Si on a trouvé le résultat
                if (MatchFound == true)
                {
                    txtResume.Text = SB.ToString();
                    UnlockForm();
                    ShowMessageBox(MessageToShow + Password);
                    return;
                }
                //Si fin du thread
                else if (BackThreadRunning == false)
                {
                    UnlockForm();
                    ShowMessageBox(MessageToShow);
                    return;
                }

                //Affichage de essais par seconde
                lblStatus.Text = "Attempts per second: " + AttemptsPerSecond.ToString();
                SecondsToComplete = (PossibleCombos - TotalAttempts) / AttemptsPerSecond;

                //Moins d une minute
                if (SecondsToComplete < 60)
                {
                    lblStatus.Text += "\nTime to End: " + SecondsToComplete.ToString() + " seconds";
                }
                //Moins d une heure
                else if (SecondsToComplete < 3600)
                {
                    TimeEstimate = (double)SecondsToComplete / 60;
                    lblStatus.Text += "\nTime to End: " + TimeEstimate.ToString("0.00") + " minutes";
                }
                //Moins d un jours
                else if (SecondsToComplete < 86400)
                {
                    TimeEstimate = (double)SecondsToComplete / 3600;
                    lblStatus.Text += "\nTime to End: " + TimeEstimate.ToString("0.00") + " hours";
                }
                //moins d un an
                else if (SecondsToComplete < 31536000)
                {
                    TimeEstimate = (double)SecondsToComplete / 86400;
                    lblStatus.Text += "\nTime to End: " + TimeEstimate.ToString("0.00") + " days";
                }
                //plus d un an
                else
                {
                    TimeEstimate = (double)SecondsToComplete / 31536000;
                    lblStatus.Text += "\nTime to End: " + TimeEstimate.ToString("0.00") + " years";
                }
                AttemptsPerSecond = 0;
            }
            catch
            {
                lblStatus.Visible = false;
            }
        }

        /// <summary>
        /// Affichage message
        /// </summary>
        /// <param name="msg"></param>
        private void ShowMessageBox(string msg)
        {
            if (this.Focused == false)
                FlashWindow(this.Handle, true);
            MessageBox.Show(this, msg, "HashCrack Time Report", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// Affichage Error
        /// </summary>
        /// <param name="msg"></param>
        private void ShowErrorBox(string msg)
        {
            if (this.Focused == false)
                FlashWindow(this.Handle, true);
            MessageBox.Show(this, msg, "HashCrack", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        /// <summary>
        /// Fermage de la fenetre
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (t != null)
                t.Interrupt();
        }

        /// <summary>
        /// Lock de la fenetre
        /// </summary>
        private void LockForm()
        {
            btnStartResume.Enabled = false;
            btnAbort.Enabled = true;
            btnExit.Enabled = false;
            txtResume.Enabled = false;
            txtHash.Enabled = false;
            txtLowerLimit.Enabled = false;
            txtUpperLimit.Enabled = false;
            chkLower.Enabled = false;
            chkUpper.Enabled = false;
            chkNumeric.Enabled = false;
            chkSpecial.Enabled = false;
            tmrMain.Enabled = true;
        }

        /// <summary>
        /// Unlock de la fenetre
        /// </summary>
        private void UnlockForm()
        {
            btnAbort.Enabled = false;
            btnStartResume.Enabled = true;
            btnExit.Enabled = true;
            txtResume.Enabled = true;
            txtHash.Enabled = true;
            txtLowerLimit.Enabled = true;
            txtUpperLimit.Enabled = true;
            chkLower.Enabled = true;
            chkUpper.Enabled = true;
            chkNumeric.Enabled = true;
            chkSpecial.Enabled = true;
            tmrMain.Enabled = false;
        }

        /// <summary>
        /// Si on quitte l'app
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnExit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        /// <summary>
        /// Annulage de la requete
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnAbort_Click(object sender, EventArgs e)
        {
            t.Interrupt();
            t1.Interrupt();

            BackThreadRunning = false;
            UnlockForm();
        }
    }
}
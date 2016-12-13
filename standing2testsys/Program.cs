using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace standing2testsys
{
    class Contest
    {
        class Problem
        {
            public string name;
            public string letter;
        }

        class Submission
        {
            public int teamId;
            public int problemId;
            public int number;
            public int time;
            public bool accepted;
        }

        string name;
        int duration = 0;
        List<Problem> problems = new List<Problem>();
        List<string> teams = new List<string>();
        List<Submission> submissions = new List<Submission>();

        public void setName(string name)
        {
            this.name = name;
        }

        public void addProblem(string name, string letter)
        {
            Problem p = new Problem();
            p.name = name;
            p.letter = letter;
            problems.Add(p);
        }

        public void addTeam(string name)
        {
            teams.Add(name);
        }

        public void addSubmission(int teamId, int problemId, int number, int time, bool accepted)
        {
            Submission s = new Submission();
            s.teamId = teamId;
            s.problemId = problemId;
            s.number = number;
            s.time = time;
            s.accepted = accepted;
            submissions.Add(s);
            duration = Math.Max(duration, s.time);
        }

        public void serialize(string file)
        {
            List<string> ls = new List<string>();
            //ls.Add("");
            ls.Add("\u001A");
            ls.Add("@contest \"" + name + "\"");
            ls.Add("@contlen " + duration);
            ls.Add("@problems " + problems.Count);
            ls.Add("@teams " + teams.Count);
            ls.Add("@submissions " + submissions.Count);
            for (int i = 0; i < problems.Count; i++)
                ls.Add("@p " + problems[i].letter + ",\"" + problems[i].name + "\",20,0");
            for (int i = 0; i < teams.Count; i++)
                ls.Add("@t " + i + ",0,1,\"" + teams[i] + "\"");
            for (int i = 0; i < submissions.Count; i++)
                ls.Add("@s " + submissions[i].teamId + "," + problems[submissions[i].problemId].letter + "," + submissions[i].number + "," +
                       submissions[i].time * 60 + "," + (submissions[i].accepted ? "OK" : "RJ"));
            File.WriteAllLines(file, ls);
        }

        public void serializeDancing(string file)
        {
            List<string> ls = new List<string>();
            ls.Add("{");
            ls.Add("  \"contestName\": \"" + name + "\",");
            ls.Add("  \"freezeTimeMinutesFromStart\": 0,");
            ls.Add("  \"problemLetters\": [");
            for (int i = 0; i < problems.Count; i++)
                ls.Add("    \"" + problems[i].letter + "\"" + (i < problems.Count - 1 ? "," : ""));
            ls.Add("  ],");
            ls.Add("  \"contestants\": [");
            for (int i = 0; i < teams.Count; i++)
                ls.Add("    \"" + teams[i] + "\"" + (i < teams.Count - 1 ? "," : ""));
            ls.Add("  ],");
            ls.Add("  \"runs\": [");
            for (int i = 0; i < submissions.Count; i++)
            {
                ls.Add("    {");
                ls.Add("      \"contestant\": \"" + teams[submissions[i].teamId] + "\",");
                ls.Add("      \"problemLetter\": \"" + problems[submissions[i].problemId].letter + "\",");
                ls.Add("      \"timeMinutesFromStart\": " + submissions[i].time + ",");
                ls.Add("      \"success\": " + submissions[i].accepted.ToString().ToLower());
                ls.Add("    }" + (i < submissions.Count - 1 ? "," : ""));
            }
            ls.Add("  ]");
            ls.Add("}");
            File.WriteAllLines(file, ls);
        }
    }    

    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "Standings (HTML, HTM)|*.html;*.htm";

            if (dialog.ShowDialog() != DialogResult.OK)
                return;

            string standingFile = dialog.FileName;

            Contest contest = new Contest();

            string standingHtml = File.ReadAllText(standingFile, Encoding.GetEncoding(1251));
            standingHtml = Regex.Match(standingHtml, "<span id=\"ajax_contestscoreboardgrid\">(.*)", RegexOptions.Singleline).Groups[1].Value;
            MatchCollection mStandingParts = Regex.Matches(standingHtml, "<table.*?>(.*?)</table>", RegexOptions.Singleline);
            string standingHeader = mStandingParts[0].Groups[1].Value;
            string standingTable = mStandingParts[1].Groups[1].Value;

            string contestName = Regex.Match(standingHeader, "<h1.*?>(.*?)</h1>", RegexOptions.Singleline).Groups[1].Value;
            contest.setName(contestName);

            MatchCollection mStandingTableRows = Regex.Matches(standingTable, "<tr.*?>(.*?)</tr>", RegexOptions.Singleline);

            MatchCollection mProblemNames = Regex.Matches(mStandingTableRows[1].Groups[1].Value, "<td.*?>(.*?)</td>", RegexOptions.Singleline);
            MatchCollection mProblemLetters = Regex.Matches(mStandingTableRows[0].Groups[1].Value, "<td.*?><b>(.*?)</b></td>", RegexOptions.Singleline);
            for (int i = 0; i < mProblemNames.Count; i++)
            {
                string name = mProblemNames[i].Groups[1].Value;                
                if (name.Contains("span"))
                    name = Regex.Match(name, "<span title=\"(.*?)\"", RegexOptions.Singleline).Groups[1].Value;
                string letter = mProblemLetters[i + 2].Groups[1].Value;
                contest.addProblem(name, letter);
            }

            for (int teamId = 0; teamId < mStandingTableRows.Count - 5; teamId++)
            {
                MatchCollection mCells = Regex.Matches(mStandingTableRows[teamId + 2].Groups[1].Value, "<td.*?>(.*?)</td>", RegexOptions.Singleline);
                contest.addTeam(mCells[1].Groups[1].Value);
                for (int problemId = 0; problemId < mProblemNames.Count; problemId++)
                {
                    if (!Regex.Match(mCells[problemId + 2].Groups[1].Value, @"\d", RegexOptions.Singleline).Success)
                        continue;
                    bool isAccepted = mCells[problemId + 2].Groups[1].Value.Contains("+");
                    string strWaCount = Regex.Match(mCells[problemId + 2].Groups[1].Value, @"[\+\-](\d+)<", RegexOptions.Singleline).Groups[1].Value;
                    int waCount = (strWaCount == "" ? 0 : int.Parse(strWaCount));
                    int hours = int.Parse(Regex.Match(mCells[problemId + 2].Groups[1].Value, @"(\d+):", RegexOptions.Singleline).Groups[1].Value);
                    int minutes = int.Parse(Regex.Match(mCells[problemId + 2].Groups[1].Value, @":(\d+)", RegexOptions.Singleline).Groups[1].Value);
                    int totalTime = hours * 60 + minutes;
                    for (int submissionNumber = 1; submissionNumber <= waCount; submissionNumber++)
                    {
                        int submissionTime = (int)Math.Round(((double)totalTime / (waCount + (isAccepted ? 1 : 0))) * submissionNumber);
                        contest.addSubmission(teamId, problemId, submissionNumber, submissionTime, false);
                    }
                    if (isAccepted)
                        contest.addSubmission(teamId, problemId, waCount + 1, totalTime, true);
                }
            }

            if (File.Exists(Path.GetDirectoryName(standingFile) + @"\contest.dat"))
                File.Delete(Path.GetDirectoryName(standingFile) + @"\contest.dat");
            contest.serialize(Path.GetDirectoryName(standingFile) + @"\contest.dat");

            if (File.Exists(Path.GetDirectoryName(standingFile) + @"\dance.json"))
                File.Delete(Path.GetDirectoryName(standingFile) + @"\dance.json");
            contest.serializeDancing(Path.GetDirectoryName(standingFile) + @"\dance.json");
        }
    }
}

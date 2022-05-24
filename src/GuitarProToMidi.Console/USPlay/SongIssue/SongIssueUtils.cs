
using System.Drawing;

namespace USPlay;
public static class SongIssueUtils
{
    public static Color GetColorForIssue(SongIssue issue)
    {
        if (issue.Severity == ESongIssueSeverity.Warning)
        {
            return Color.Yellow;
        }
        else
        {
            return Color.Red;
        }
    }
}

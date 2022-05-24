using MidiExport;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using USPlay;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public static class SongEditorMidiFileImporter 
{
    //public static void ImportMidiFile(string midiFilePath)
    //{
        
    //    if (!File.Exists(midiFilePath))
    //    {
    //        return;
    //    }

    //    try
    //    {
    //        List<Note> loadedNotes = LoadNotesFromMidiFile(midiFilePath);
    //        foreach (Note loadedNote in loadedNotes)
    //        {
    //        }
    //    }
    //    catch (Exception e)
    //    {
    //    }
    //}

    //private static List<Note> LoadNotesFromMidiFile(string midiFilePath)
    //{
    //    List<Note> loadedNotes = new List<Note>();
    //    //  var midiFile = MidiMusic.Read(System.IO.File.OpenRead(midiFilePath));
    //    if (midiFile == null)
    //    {
    //        throw new Exception("Loading midi file failed.");
    //    }

    //    Dictionary<int, USPlay.Note> midiPitchToNoteUnderConstruction = new Dictionary<int, USPlay.Note>();
    //    return NewMethod(loadedNotes, midiPitchToNoteUnderConstruction);
    //}

    public static List<USPlay.Note> LoadNodes(SongMeta songMeta, List<MidiTrack> tracks)
    {
        Dictionary<int, USPlay.Note> midiPitchToNoteUnderConstruction = new Dictionary<int, USPlay.Note>();
        List<USPlay.Note> loadedNotes = new List<USPlay.Note>();
        foreach (var midiEvent in tracks[1].messages)
        {
            if (midiEvent.type == "note_on")
            {
                HandleStartOfNote(songMeta, midiEvent, midiPitchToNoteUnderConstruction);
            }

            if (midiEvent.type == "note_off")
            {
                HandleEndOfNote(songMeta, midiEvent, midiPitchToNoteUnderConstruction, loadedNotes);
            }
        }
        return loadedNotes;
    }

    private static void HandleEndOfNote(SongMeta songMeta, MidiMessage midiEvent, Dictionary<int, USPlay.Note> midiPitchToNoteUnderConstruction, List<USPlay.Note> loadedNotes)
    {
        int midiPitch = midiEvent.pitch;
        int deltaTimeInMillis = GetDeltaTimeInMillis(songMeta, midiEvent);
        if (midiPitchToNoteUnderConstruction.TryGetValue(midiPitch, out USPlay.Note existingNote))
        {
            int endBeat = (int)BpmUtils.MillisecondInSongToBeat(songMeta, deltaTimeInMillis);
            if (endBeat > existingNote.StartBeat)
            {
                existingNote.SetEndBeat(endBeat);
                loadedNotes.Add(existingNote);
            }
            else
            {
              //  Debug.LogWarning($"End beat {endBeat} is not after start beat {existingNote.StartBeat}. Skipping this USPlay.Note.");
            }
            midiPitchToNoteUnderConstruction.Remove(midiPitch);
        }
        else
        {
          //  Debug.LogWarning($"No USPlay.Note for pitch {midiPitch} is beeing constructed. Ignoring this USPlay.Note_Off event.");
        }
    }

    private static void HandleStartOfNote(SongMeta songMeta, MidiMessage midiEvent, Dictionary<int, USPlay.Note> midiPitchToNoteUnderConstruction)
    {
        int midiPitch = midiEvent.pitch;
        int deltaTimeInMillis = GetDeltaTimeInMillis(songMeta, midiEvent);
        USPlay.Note newNote = new USPlay.Note();
        int startBeat = (int)BpmUtils.MillisecondInSongToBeat(songMeta, deltaTimeInMillis);
        newNote.SetStartAndEndBeat(startBeat, startBeat);
        newNote.SetMidiNote(midiPitch);

        if (midiPitchToNoteUnderConstruction.ContainsKey(midiPitch))
        {
          //  Debug.LogWarning($"A USPlay.Note with pitch {midiPitch} started but did not end before the next. The USPlay.Note will be ignored.");
        }

        midiPitchToNoteUnderConstruction[midiPitch] = newNote;
    }

    private static int GetDeltaTimeInMillis(SongMeta songMeta, MidiMessage midiEvent)
    {
        int deltaTimeInSamples = midiEvent.timeOrg;
        int bpm = 82;
        int quarterTime = Duration.quarterTime;
        int deltaTimeInMillis = (int)((60000.0f / (quarterTime * bpm)) * deltaTimeInSamples);

       // int midiStreamSampleRateHz = 44100;
       // int deltaTimeInMillis = (int)(deltaTimeInSamples / (midiStreamSampleRateHz / 1000));
        return deltaTimeInMillis;
    }
}

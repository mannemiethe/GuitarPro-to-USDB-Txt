using System.IO;
using System.Collections.Generic;
using NLog;
using USPlay;
using System;

namespace GuitarProToMidi
{
    public class GpFileParser
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly string _filePath;
        private readonly bool _extract_All;
        private readonly string _extension;
        private GPFile _gpfile;

        public GpFileParser(string filePath, bool extractAll)
        {
            _filePath = filePath;
            _extract_All = extractAll;
            _extension = Path.GetExtension(filePath);
        }

        public void loadGPFile()
        {
            var loader = File.ReadAllBytes(_filePath);

            switch (_extension)
            {
                case ".gp3":
                    _gpfile = new GP3File(loader);
                    _gpfile.readSong();
                    break;
                case ".gp4":
                    _gpfile = new GP4File(loader);
                    _gpfile.readSong();
                    break;
                case ".gp5":
                    _gpfile = new GP5File(loader);
                    _gpfile.readSong();
                    break;
                case ".gpx":
                    _gpfile = new GP6File(loader);
                    _gpfile.readSong();
                    _gpfile = _gpfile.self; //Replace with transferred GP5 file
                    break;
                case ".gp":
                    var stream = new MemoryStream();
                    using (var unzip = new Unzip(_filePath))
                    {
                        unzip.Extract("Content/score.gpif", stream);
                        stream.Position = 0;
                        var sr = new StreamReader(stream);
                        var gp7Xml = sr.ReadToEnd();

                        _gpfile = new GP7File(gp7Xml);
                        _gpfile.readSong();
                        _gpfile = _gpfile.self; //Replace with transferred GP5 file
                    }

                    break;
                default:
                    Logger.Error("Unknown File Format");
                    break;
            }
        }

        public List<MidiExport.MidiTrack> OnlyOnMidiMessagesForUsDx(Native.Format song)
        {
            var Bpm = _gpfile.tempo;
            int quarterTime = Duration.quarterTime;
            var midiOnNodes = song.getMidiTracksWithOnNode(Bpm, quarterTime);
            return midiOnNodes;
        }


        public List<USPlay.Voice> GetVoices(List<MidiExport.MidiTrack> midiTracks){

            List<USPlay.Voice> voices = new List<USPlay.Voice >();
            foreach (var midiTrack in midiTracks)
            {
                if (midiTrack.messages != null && midiTrack.messages.Count > 0)
                {
                    if (String.IsNullOrWhiteSpace(midiTrack.name))
                    {
                        midiTrack.name = "";
                    }
                    var voice = new USPlay.Voice(GetSentences(midiTrack.messages), midiTrack.name);

                    if (_extract_All || midiTrack.IsVocal)
                    {
                        voices.Add(voice);
                    }
                }
            }
            return voices;
        }
       
        public List<Sentence> GetSentences(List<MidiExport.MidiMessage> midiMessages)
        {
            List<Sentence> sentences = new List<Sentence>();
            int sentenceIndex = -1;
            Sentence currentSentence = null;
            USPlay.Note lastNote = null;
            int currentNode = 0;
            foreach (var midiMessage in midiMessages)
            {
                if (midiMessage.measureIndex > sentenceIndex)
                {
                    sentenceIndex = midiMessage.measureIndex;
                    currentSentence = new Sentence();
                    sentences.Add(currentSentence);

                }
                if(midiMessage.noteText == " day")
                {

                }
 
                var note=new USPlay.Note(ENoteType.Normal, midiMessage.timeOrgInUsDxBeat, midiMessage.timeDurationInUsDxBeat, midiMessage.noteInUsDx, midiMessage.noteText != null ? midiMessage.noteText : "");
                if (lastNote != null)
                {
                    if (lastNote.StartBeat < note.StartBeat)
                    {
                        lastNote = note;
                        currentSentence.AddNote(note);
                    }
                    else
                    {

                    }
                }
                else
                {
                    lastNote = note;
                    currentSentence.AddNote(note);
                }
                currentNode++;


            }
            return sentences;
        }


        public SongMeta GetSongMetaOfGpFile()
        {
            SongMeta songMeta=null;
            try
            {
                var song = new Native.Format(_gpfile);
                var voicesDict = new Dictionary<string, string>();
                var tracks=OnlyOnMidiMessagesForUsDx(song);
                songMeta = new SongMeta(Path.GetDirectoryName(_filePath), $"{Path.GetFileNameWithoutExtension(_filePath)}.txt", "", _gpfile.interpret, _gpfile.tempo, "", _gpfile.title, voicesDict, System.Text.Encoding.UTF8);


                var voices = this.GetVoices(tracks);
                foreach (var voice in voices) {
                    songMeta.AddVoice(voice);
                }
                var bpm = _gpfile.tempo;
                int quarterTime = Duration.quarterTime;

                if (tracks.Count > 0)
                {
                    foreach (var track in tracks)
                    {
                        if (track.IsVocal)
                        {
                            songMeta.Gap = song.getMidiAsSeconds(track.messages[0].timeOrg, bpm, quarterTime);
                        }
                    }
                }

            }
            catch (Exception e)
            {

            }
            return songMeta;
        }

        public void WriteUsDxSongFromGpFile()

        {

            loadGPFile();
            var outPutFilePath = Path.Join(Path.GetDirectoryName(_filePath),
                        $"{Path.GetFileNameWithoutExtension(_filePath)}.txt");
            UltraStarSongFileWriter.WriteFile(outPutFilePath, GetSongMetaOfGpFile());
        }

        public byte[] CreateMidiFile()
        {

            loadGPFile();
            Logger.Debug("Done");

            var song = new Native.Format(_gpfile);
            return song.ToMidi().createBytes().ToArray();
        }
    }
}

using System.Text;

/*
    ::: AUTHOR      : <<| Joshua Newell Diehl |>>
    ::: VERSION     : 0.1.0
    ::: Last Updated: 11-24-2023

    Merge and sort input files produced by lhtrace
    
 */
namespace TraceMerge
{ 
    enum Command
    {
        Merge   = 1,
        Help    = 0,
        Invalid = -1
    }

    // Sentries check for and reject bad input
    class Sentry 
    {
        public static void ArgsLength(int len) 
        {
            if (len == 0) { throw new ArgumentException("Received empty input."); } 
        }
        
        public static void MergeArgsLength(int len)
        {
            if (len < 3) { throw new ArgumentException("Merging requires a minimum of 3 arguments."); }
        }
    }

    internal class Program
    {
        static int Main(string[] args)
        {
            int numArgs = args.Length;

            try
            {
                Sentry.ArgsLength(numArgs);
                Command cmd = MatchCommand(args[0]); // Treat first argument as command

                if (cmd == Command.Help)
                {
                    Usage();
                    return 0;
                };

                if (cmd == Command.Merge)
                {
                    Sentry.MergeArgsLength(numArgs); 
                    var inputPaths = CollectPaths(args);

                    string decoded = "";

                    foreach(var inputPath in inputPaths)
                    {
                        decoded += DecodeUTF16le(inputPath);
                    }

                    string mergeResult  = new Frame.Collection(decoded.Split("\r\n\r\n")).Merge();

                    WriteMerged(mergeResult);                
                }
            }
            catch (Exception e)
            {
                Usage();
                Console.WriteLine(e.ToString());
                return 1;
            } 

            return 0;
        }

        private static void Usage()
        {
            Console.WriteLine("| TRACEMERGE |\n");
            Console.WriteLine("Usage:");
            Console.WriteLine("{<>.exe} <command> <argument(s)>");
            Console.WriteLine("\nCOMMANDS ::: ");
            Console.WriteLine("\tmerge - pass variable # of paths to trace files to be merged");
            Console.WriteLine("\t\tExample: {<>.exe} merge file1.txt file2.txt file3.txt ...");
            Console.WriteLine("\thelp - display this help menu");
            Console.WriteLine("\t\tExample: {<>.exe} help\n\n");
        }

        // Produces enum variant for input command
        private static Command MatchCommand(string cmd) =>
            cmd.Trim().ToLower() switch
            {
                "help" or "h" => Command.Help,
                "merge" or "m" => Command.Merge,
                _ => Command.Invalid
            };

        // On merge command, consider all remaining inputs filepaths
        // (Needs work)
        private static string[] CollectPaths(string[] args) => args[1..^0];

        // Decode log format into workable string
        private static string DecodeUTF16le(string path)
        {
            byte[] fileBytes = File.ReadAllBytes(path);
            
            return Encoding.Unicode.GetString(fileBytes, 0, fileBytes.Length); 
        }

        // Write merged frames to working directory
        private static void WriteMerged(string merged)
        {
            using (StreamWriter outputFile = new("merged.txt"))
            {
                outputFile.Write(merged);
            }
        }

    }
}

// Handles formatting and parsing of individual frames to enable sorting
namespace Frame
{
    // Structure representing an individual frame with sortable `post` key
    public class Item
    {
        public string pre;
        public DateTime post;
        public string frame;
    
        // Parse string into sortable DateTime object
        private static DateTime FormatTimeString(string tString) =>
            DateTime.ParseExact(tString.Substring(2, 12).Trim() + '0', "HH:mm:ss.fff", null);
    
        public Item(string pre, string frame)
        {
            this.pre = pre;
            this.post = FormatTimeString(pre);
            this.frame = frame;
        }
    }
    
    // Represents and manages a List collection of Frame.Item
    public class Collection 
    {
        private List<Item> frames = new();
        public Collection(string[] frameItems)
        {
            // Populates self with constructed frame items
            foreach(var item in frameItems)
            {
                if (item == "") break;

                var tString = item[..14];
                var fItem = new Item(tString, item[14..]);
    
                frames.Add(fItem);
            }

            SortByTime();
        }
    
        // Sort list of Frame.Item by `post` property
        private void SortByTime()
        {
            this.frames.Sort((f1, f2) => f1.post.CompareTo(f2.post));
        }

        // Merge sorted `this.frames` into formatted writable string
        public string Merge()
        {
            List<string> result = new();

            foreach(var frame in this.frames)
            {
                result.Add(frame.pre + frame.frame);
            }

            return string.Join("\r\n\r\n", result.ToArray());
        }
    } 
}
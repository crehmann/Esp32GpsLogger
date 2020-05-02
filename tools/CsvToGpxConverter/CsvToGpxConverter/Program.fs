open FSharp.Data
open System
open System.Xml.Linq
open System.Xml
open System.Text
open System.IO
open Argu
open System.Xml

[<Literal>] 
let GpxXsd = "https://www.topografix.com/gpx/1/1/gpx.xsd"
[<Literal>] 
let GpxVersion = "1.1"
[<Literal>] 
let GpxNamespace = "http://www.topografix.com/GPX/1/1"

type CliArguments = 
    | [<Mandatory>] InputFile of string
    | OutputFile of string
    interface IArgParserTemplate with
        member s.Usage = 
            match s with
            | InputFile _ -> "specify the input file (csv)."
            | OutputFile _ -> "specify the output file (gpx)."

type GpsLogs = CsvProvider< Schema="Timestamp (date), Latitude (decimal), Longitude (decimal), Altitude (decimal option), Hdop (decimal option), Satellites (string option)", HasHeaders=false >

type Gpx = XmlProvider< Schema= GpxXsd>

let GpxDocument(trackpoints : seq<Gpx.Trkpt>) = 
    let trackSegment = Gpx.Trkseg((trackpoints |> Seq.toArray), Option.None)
    let track = 
        Gpx.Trk
            (Option.None, Option.None, Option.None, Option.None, Array.empty, 
             Option.None, Option.None, Option.None, [|trackSegment|])
    let gpx = XElement(XName.Get("gpx", GpxNamespace), track.XElement)
    gpx.SetAttributeValue(XName.Get("version"), GpxVersion :> obj)
    XDocument(XDeclaration("1.0", "UTF-8", "yes"), [|gpx :> obj|])

let convertRowToWpt(row : GpsLogs.Row) = 
    Gpx.Trkpt
        (row.Latitude, row.Longitude, row.Altitude, 
         Some(DateTimeOffset(row.Timestamp)), None, None, None, None, None, None, 
         Array.empty, None, None, None, row.Satellites, row.Hdop, None, None, 
         None, None, None)

let convertToGpx (inputFilePath : string) (outputFilePath : string) = 
    let inputFileStream = File.OpenText inputFilePath
    let rows : seq<GpsLogs.Row> = (GpsLogs.Load inputFileStream).Rows
    let wpts = Seq.map convertRowToWpt rows
    let doc = GpxDocument wpts
    let output = File.OpenWrite(outputFilePath)
    use xtw = 
        XmlWriter.Create
            (output,              
             XmlWriterSettings
                 (Encoding = Encoding.UTF8, Indent = true, 
                  CheckCharacters = true))
    doc.WriteTo(xtw)

let validateFilePaths (cliArguments:ParseResults<CliArguments>) =
    let inputFileOption = cliArguments.TryGetResult InputFile
    let outputFile = cliArguments.GetResult (OutputFile, defaultValue = "output.gpx")
    match inputFileOption with
    | None -> Error "InputFile must be specified"
    | Some inputFile when not (File.Exists inputFile) -> Error "Input file does not exists"
    | Some inputFile -> Ok {|InputFilePath = inputFile; OutputFilePath = outputFile|}

[<EntryPoint>]
let main argv = 
    let errorHandler = ProcessExiter(colorizer = function ErrorCode.HelpText -> None | _ -> Some ConsoleColor.Red)
    let parser = ArgumentParser.Create<CliArguments>(errorHandler = errorHandler)
    let cliArguments = parser.ParseCommandLine(argv)
    let filePathResult = validateFilePaths cliArguments
    match filePathResult with
    | Error e -> 
        cliArguments.Raise e
        1
    | Ok paths -> 
        convertToGpx paths.InputFilePath paths.OutputFilePath
        printfn "File successfully converted"
        0
open FSharp.Data
open System
open System.Xml.Linq
open System.Xml
open System.Text
open System.IO
open Argu

type CliArguments = 
    | [<Mandatory>] InputFile of string
    | OutputFile of string
    interface IArgParserTemplate with
        member s.Usage = 
            match s with
            | InputFile _ -> "specify the input file (csv)."
            | OutputFile _ -> "specify the output file (gpx)."

type GpsLogs = CsvProvider< Schema="Timestamp (date), Latitude (decimal), Longitude (decimal), Altitude (decimal option), Hdop (decimal option), Satellites (string option)", HasHeaders=false >

type Gpx = XmlProvider< Schema="https://www.topografix.com/gpx/1/1/gpx.xsd" >

let XDeclaration version encoding standalone = 
    XDeclaration(version, encoding, standalone)

let XDocument xdecl content = 
    XDocument(xdecl, 
              content
              |> Seq.map(fun v -> v :> obj)
              |> Seq.toArray)

let convertRowToWpt(row : GpsLogs.Row) = 
    Gpx.Wpt
        (row.Latitude, row.Longitude, row.Altitude, 
         Some(DateTimeOffset(row.Timestamp)), None, None, None, None, None, None, 
         Array.empty, None, None, None, row.Satellites, row.Hdop, None, None, 
         None, None, None)

let convertToGpx (inputFilePath : string) (outputFilePath : string) = 
    let inputFileStream = File.OpenText inputFilePath
    let rows : seq<GpsLogs.Row> = (GpsLogs.Load inputFileStream).Rows
    let wpts = Seq.map convertRowToWpt rows
    let doc = XDocument (XDeclaration "1.0" "UTF-8" "yes") wpts
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
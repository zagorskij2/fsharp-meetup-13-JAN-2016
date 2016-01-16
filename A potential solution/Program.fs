open System
open System.IO

[<EntryPoint>]
let main argv =
    printfn "%A" argv

    let clean (potentialWord:string) =
        [ "_"; "."; "["; "]"; "\\"; "$"; "#"; "!"; "?"; "@"; "*"; ","; "("; ")"; "\""; "%"; ":"; ";";
          "'"; "|"; "/"; "^"; ","; ]
        |> List.fold(fun (acc:string) c -> acc.Replace(c,"")) potentialWord

    let filterBlanks (word:string) =
        if String.IsNullOrEmpty(word)
        then None
        else Some(word)

    let replaceNewLines (line:string) =
        line.Replace("\n"," ")

    let splitOnSpace (text:string) =
        text.Split([|' '|])

    let lowerCase (text:string) =
        text.ToLower()

    let getWords (line:string) =
        line
        |> replaceNewLines
        |> splitOnSpace
        |> Seq.map clean
        |> Seq.map lowerCase
        |> Seq.choose filterBlanks
        |> List.ofSeq

    let chuckPostamble (text:string) = 
        let pos = text.IndexOf("END OF THIS PROJECT GUTENBERG EBOOK")
        if pos > 0
        then text.Substring(0,pos)
        else text

    let chainLen = 4

    let getFingerprint (fileName:string) =
        File.ReadAllText(fileName)
        |> chuckPostamble
        |> getWords
        |> Seq.windowed chainLen
        |> Seq.map(fun words -> System.String.Join(" ",words))
        |> List.ofSeq

    let basePath = @"C:\fsharp-meetup-13-JAN-2016-master"

    let justFileName (filePath:string) = 
        let fi = new FileInfo(filePath)
        fi.Name

    let knowns = 
        Directory.GetFiles(Path.Combine(basePath,"Known Files"))
        |> Array.Parallel.map(fun f -> (f |> justFileName, getFingerprint f))
    let unknowns = 
        Directory.GetFiles(Path.Combine(basePath,"Mystery Files"))
        |> Array.Parallel.map(fun f -> (f |> justFileName, getFingerprint f))

    // I've put this here to demonstrate something important.
    // The function "slowSimilarity" is filtering those elements
    // of the first list that exist in the second list.  It's a straight-forward-looking
    // approach, but it is very slow!  For cases like these, the better approach
    // is to use the built-in set theory functionality of F#.  The function
    // "similarity" tears through the work in just a few seconds on my laptop.
    // Smart use of F# for the win!

    let slowSimilarity (chains1:list<string>) (chains2:list<string>) =
        chains1
        |> List.filter(fun chain -> chains2 |> List.exists(fun c -> c = chain) )
        |> List.length     
        
    let similarity (fingerPrint1:list<string>) (fingerPrint2:list<string>) = 
        let s = 
            Set.intersect (set fingerPrint1) (set fingerPrint2) 
        s |> Set.count
        

//    Here we are comparing known files to other known files to get a warm fuzzy feeling
//    about the approach:

(*

    let knownComparisons = 
        [0 .. knowns.Length - 1]
        |> List.collect(fun k1 -> 
            [0 .. knowns.Length - 1]
            |> List.map(fun k2 -> (knowns.[k1], knowns.[k2])))

    use w = new StreamWriter(Path.Combine(basePath,"knowns_compared.txt"))

    knownComparisons
    |> List.map(fun ((label1,fingerPrint1), (label2,fingerPrint2)) ->
        printfn "Comparing %s to %s" label1 label2 
        (label1, label2, similarity fingerPrint1 fingerPrint2) )
    |> List.sortBy(fun (l1, l2, c) -> l1, c)
    |> List.iter(fun (l1, l2, c) -> 
        w.WriteLine(sprintf "%s x %s: %i" l1 l2 c))

    w.Close()        
*)

    let knownToUnknownComparisons = 
        [0 .. knowns.Length - 1]
        |> List.collect(fun k -> 
            [0 .. unknowns.Length - 1]
            |> List.map(fun u -> (knowns.[k], unknowns.[u])))

    use w = new StreamWriter(Path.Combine(basePath,"knowns_to_unknowns_compared.txt"))

    knownToUnknownComparisons 
    |> List.map(fun ((label1,fingerPrint1), (label2,fingerPrint2)) ->
        printfn "Comparing %s to %s" label1 label2 
        (label1, label2, similarity fingerPrint1 fingerPrint2) )
    |> List.sortBy(fun (l1, l2, c) -> l1, c)
    |> List.iter(fun (l1, l2, c) -> 
        w.WriteLine(sprintf "%s x %s: %i" l1 l2 c))

    w.Close()

    0 // return an integer exit code
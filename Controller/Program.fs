
module Program =
    
    open System
    open System.Diagnostics
    open System.Threading.Tasks
    open System.Windows.Media

    open Elmish
    open Elmish.WPF

    open FSharp.Control.Reactive


    type private GridButtonCorner =
        | TL
        | TR
        | BL
        | BR

    type private GameEvent =
        | Start
        | Restart
        | LevelComplete
        | PlayingSequenceComplete
        | GridButtonMouseDown of GridButtonCorner

    type private GameState =
        | AwaitingUserResponse
        | PlayingSequence
        | NotStarted
        | GameOver

    type private GameModel =
        {
            State: GameState
            Level: int
            History: GridButtonCorner list
            RemainingUserSelects: GridButtonCorner list
        }


    let private init () =
        let model =
            {
                State = NotStarted
                Level = 0
                History = []
                RemainingUserSelects = []
            }

        model, Cmd.none


    let private getNextSelection (rnd: Random) () =
        match rnd.Next (3) with
        | 0 -> TL
        | 1 -> TR
        | 2 -> BL
        | 3 -> BR
        | _ -> failwith "Unknown corner generated."


    let private (|AsPath|) (mainWindow: View.MainWindow) = function
        | TL -> mainWindow.TL
        | TR -> mainWindow.TR
        | BL -> mainWindow.BL
        | BR -> mainWindow.BR


    let private update (mainWindow: View.MainWindow) =
        let rnd = new Random (100)

        let selectionPicker = getNextSelection rnd

        let (|AsPath|) = (|AsPath|) mainWindow

        let playSequence seq =
            let rec inner = function
            | (AsPath p)::xs ->
                async {
                    let tcs = new TaskCompletionSource()                    
                    // The event handler MUST be registered before the animation begins.
                    use evt = mainWindow.FlashGridButtonAnimation.Completed.Subscribe (fun _ -> tcs.SetResult())
                    do mainWindow.FlashGridButtonAnimation.Begin (p)
                    do! Async.AwaitTask tcs.Task
                    do evt.Dispose ()
                    do! Async.Sleep 500
                    do! inner xs
                }

            | [] ->
                async.Zero ()

            async {                
                do! Async.Sleep 500
                do mainWindow.Grid.Fill <- Brushes.DodgerBlue
                do! Async.Sleep 500
                do! inner seq
                do mainWindow.Grid.Fill <- Brushes.Black
            }

        fun msg state ->
            sprintf "Message recieved: %A" msg
            |> Debug.WriteLine

            match state, msg with
            | { State = NotStarted }, Start ->
                state, Cmd.ofMsg LevelComplete

            | _, LevelComplete ->
                let nextItem = selectionPicker ()

                let newSequence = state.History @ [nextItem]

                let newState =
                    { state with State = PlayingSequence; Level = state.Level + 1; History = newSequence; RemainingUserSelects = newSequence }
                    
                newState, Cmd.OfAsync.perform playSequence newSequence (fun _ -> PlayingSequenceComplete)

            | { State = NotStarted }, _ ->
                state, Cmd.none

            | { State = PlayingSequence }, PlayingSequenceComplete ->
                { state with State = AwaitingUserResponse }, Cmd.none

            | { State = PlayingSequence }, _ ->
                state, Cmd.none

            | { State = AwaitingUserResponse; RemainingUserSelects = btn::btns }, GridButtonMouseDown (AsPath p as btn') ->
                do mainWindow.FlashGridButtonAnimation.Begin (p)

                if btn = btn' then                 
                    match btns with
                    | [] ->
                        do mainWindow.FlashGridSuccessAnimation.Begin (mainWindow.Grid)

                        state, Cmd.ofMsg LevelComplete
                    | _ ->
                        { state with RemainingUserSelects = btns }, Cmd.none

                else
                    do mainWindow.FlashGridFailureAnimation.Begin (mainWindow.Grid)

                    { state with State = GameOver }, Cmd.none                

            | _, _ ->
                sprintf "Invalid state (%A) and message (%A) combination." state msg
                |> Debug.WriteLine

                state, Cmd.none


    let private bindings () = [
        "IsAwaitingUserResponse" |> Binding.oneWay (function
            | { State = AwaitingUserResponse } -> true | _ -> false)

        "Start" |> Binding.cmdIf (function
            | { State = NotStarted } -> Some Start | _ -> None)
    ]


    let private subscriptions (mainWindow: View.MainWindow) _ =
        let (|AsPath|) = (|AsPath|) mainWindow

        Cmd.ofSub (fun dispatch ->
            [TL; TR; BL; BR]
            |> List.iter (fun (AsPath p as btn) ->
                p.MouseLeftButtonDown
                |> Observable.map (fun _ -> btn)
                |> Observable.add (GridButtonMouseDown >> dispatch)))


    [<EntryPoint>]
    [<STAThread>]
    let main _ =
        let mainWindow =
            new View.MainWindow()

        Program.mkProgramWpf init (update mainWindow) bindings
        |> Program.withSubscription (subscriptions mainWindow)
        |> Program.runWindow mainWindow
﻿namespace Fiffi.Fx
open Fiffi

module State =
    let RehydrateStateFrom<'TState> (events: IEvent list) (defaultState:'TState) f: 'TState =
        List.fold f defaultState events

module EventStore =

    type Version =
        | Version of int64

    type StreamName =
        | StreamName of string

    let apply f (Version e) = f e
    let value e = apply id e

    type Load = IEventStore -> StreamName -> Version -> Async<Version * IEvent list>

    type Append = IEventStore-> StreamName -> Version -> IEvent list -> Async<Version>

    let load : Load = fun store streamName version ->
        async {
            let (Version v) = version
            let (StreamName sn) = streamName
            let! struct(events,version) = store.LoadEventStreamAsync(sn, v) |> Async.AwaitTask
            return (version |> Version, events |> List.ofSeq )
            }

    let append : Append = fun store streamName version events ->
        async {
            let (Version v) = version
            let (StreamName sn) = streamName
            let! version = store.AppendToStreamAsync(sn, v, events |> Seq.toArray) |> Async.AwaitTask
            return version |> Version
        }

module App =
   
    let mapAsync f a = async { 
        let! v = a
        return (f v)
    }

    type Append = EventStore.StreamName -> EventStore.Version -> IEvent list -> Async<EventStore.Version>
    type AsyncAppend = EventStore.StreamName -> Async<EventStore.Version * IEvent list> -> Async<EventStore.Version>
    type Read = EventStore.StreamName -> EventStore.Version -> Async<EventStore.Version * IEvent list>
    type Handler<'TCommand, 'TState> = 'TCommand -> EventStore.Version * 'TState -> EventStore.Version * IEvent list
    type AsyncHandler<'TCommand, 'TState> = 'TCommand -> Async<EventStore.Version * 'TState> -> Async<EventStore.Version * IEvent list>
    type AsyncRehydrate<'T> = Async<EventStore.Version * IEvent list> -> Async<EventStore.Version * 'T>

    let asyncRehydrate<'TState> (defaultState:'TState) f : AsyncRehydrate<'TState> = 
        fun x -> x |> mapAsync(fun(version, events) -> (version ,(State.RehydrateStateFrom events defaultState f)))
    
    //let asyncHandler (h:Handler<ICommand, GameState>) : AsyncHandler<ICommand, GameState> = 
    //    fun cmd state -> state |> mapAsync(fun(state) -> (h cmd state))
    
    let asyncAppend (a:Append) : AsyncAppend = fun streamName t -> 
        let appender = a(streamName)
        async {
         let! (version, events) = t
         return! appender version events
        }

    let applicationService<'TCommand, 'TState> (store) (defaultState:'TState) (apply) (cmd:'TCommand) (cmdMeta:ICommand) (f) : Async<unit> =
        let startFrom = (int64 0) |> EventStore.Version

        let append : Append = fun streamName expectedVersion events -> 
           let a = EventStore.append store
           a streamName expectedVersion events
        
        let read : Read = fun streamName version ->
            let l = EventStore.load store
            l streamName version

        let aggregateStreamReader (cmd:ICommand) = read (cmd.AggregateId.ToString() |> EventStore.StreamName) startFrom
        let aggregateStreamAppender (cmd:ICommand) = asyncAppend append (cmd.AggregateId.ToString() |> EventStore.StreamName)

        cmdMeta //TODO to streamName
        |> aggregateStreamReader
        |> asyncRehydrate defaultState apply
        |> fun(x) -> 
            async {
                let! (version, state) = x
                let e = f state cmd
                return (version, e)
            }
        |> aggregateStreamAppender cmdMeta 
        |> Async.Ignore  
 
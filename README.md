# conversation-service

Conversations and groups: who is in which chat, with which role, and settings such as the disappearing-message timer. It also handles message requests, so a stranger's first message waits until you accept it.

The state is event sourced. Every change is stored as an event, the read models are projections of those events, and they can be rebuilt by replaying the history. Other services ask it about membership over gRPC.

Built with .NET 10, Marten on PostgreSQL and gRPC.

Owner: Amar Mehić. Status: planned.

## Where this fits

This is one of the repositories of [Pretty Good Pigeon](https://github.com/rso-ekipa13), an end-to-end encrypted messenger built by ekipa 13 for the Cloud Computing course at UL FRI (2026/27). The architecture, plan and decisions live in [docs](https://github.com/rso-ekipa13/docs).

`main` only changes through pull requests; day-to-day work goes to `dev`.

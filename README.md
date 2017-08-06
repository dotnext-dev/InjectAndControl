# InjectAndControl
Sample app that highlights the ability to use hooking/inject and win-proc to control 3rd party windows apps

Uses binaries from https://github.com/cplotts/snoopwpf

In reference to answer : https://stackoverflow.com/a/45453043/7292772

| Project  | Description |
| ------------- | ------------- |
| Interceptor.WindowsForms  | Setup ultility for adding a message (WndProc) filter to a windows-forms app  |
| Interceptor.WPF  | Setup ultility for adding a message (WndProc) filter to a WPF app  |
| Injector.Common  | Utility to inject a dll into windows-forms or WPF app and invoke a static method (uses managed-injector libraries from SnoopUI project - https://github.com/cplotts/snoopwpf)  |
| Samples.WpfApp  | Sample WPF app to inject external dll using Injector  |
| Samples.WindowsFormsApp  | Sample windows forms app to inject external dll to using Injector  |
| EventBlockerApp  | Uses above utililty libraries to inject and add event blockers to sample apps  |

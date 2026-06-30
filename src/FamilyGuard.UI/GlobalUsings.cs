// Resolve ambiguity between System.Windows.Application (WPF)
// and System.Windows.Forms.Application (WinForms) when UseWindowsForms is enabled.
global using Application = System.Windows.Application;

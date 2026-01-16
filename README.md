# Method Invoker - Unity

> Tired of building throwaway GUIs for component testing?

Method-Invoker is a powerful visual tool purpose-built for Unity that lets you call component methods on the fly.

Easy to install, zero dependencies, and ready to use out of the box.

![](/docs/image/gui.gif)


## Install - Unity Package Manager

- Navigate to `Window` > `Package Manager`
- Click `+` -> `Add package from git URL...`
- Paste the following URL:
    ```
    https://github.com/misk0225/Method-Invoker-Unity.git
    ```


## Use

- Navigate to `Tools` > `Method Invoker`
- Click on any GameObject in your Hierarchy or Scene view.
- Choose the method you want to invoke and enjoy!


## Features

- Support private mathods.
- Support for multi-parameter methods.
- Serialization of different objects:
    - ✔️ basic types
    - ✔️ object / struct
    - ✔️ unity types
    - ✔️ Unity.Object Refrence
    - ✔️ array
    - ✔️ enum
    - ❌ list
    - ❌ dictionary
    - ❌ delegate
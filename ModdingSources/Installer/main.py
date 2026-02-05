import os
import shutil
import sys
from typing import Any, Callable, cast
import requests
from options import load_config, parse_options, save_options
from launcher import launcher
from platformdirs import user_config_dir
from mods import AVAILABLE_MODS
from constants import INSTALLER_DOWNLOAD_URL
import logging


logging.basicConfig(
    level=logging.INFO,
    format="[%(asctime)s][%(levelname)s] %(message)s",
    filename=os.path.join(user_config_dir("ToppleBitModding", ensure_exists=True), "installer.log"),
)


def main():
    options = parse_options()
    logging.info("Parsed options: %s", vars(options))
    global tk, messagebox, filedialog
    if options.no_window:
        logging.info("Running installer in no-window mode.")
    else:
        import tkinter as tk
        from tkinter import messagebox, filedialog

        logging.info("Running installer with GUI.")

    already_installed = False
    if options.installer_install_path:
        if os.path.exists(options.installer_install_path):
            already_installed = True

    if not options.setting_save_path and os.path.exists(
        os.path.join(user_config_dir("ToppleBitModding"), "config")
    ):
        already_installed = True
        config_path = os.path.join(user_config_dir("ToppleBitModding"), "config")
        with open(config_path, "r") as f:
            options.setting_save_path = f.read().strip()

    if options.setting_save_path:
        if os.path.exists(options.setting_save_path):
            already_installed = True
            config = load_config(options.setting_save_path)
            for key, value in config.items():
                if (
                    hasattr(options, key)
                    and getattr(options, key) is None
                    or getattr(options, key) == []
                    or getattr(options, key) is False
                ):
                    setattr(options, key, value)

    if any([mod not in AVAILABLE_MODS for mod in options.mod_list]):
        if not options.no_window:
            messagebox.showerror(
                "Error", "One or more specified mods are not available."
            )
        logging.error("Error: One or more specified mods are not available.")
        sys.exit(1)

    if already_installed:
        logging.info("Installer already installed or settings file found. Skipping setup.")
        launcher(options)
        return

    if not options.setting_save_path:
        options.setting_save_path = os.path.join(
            user_config_dir("ToppleBitModding", ensure_exists=True), "settings.yaml"
        )

    if not options.installer_install_path:
        options.installer_install_path = user_config_dir(
            "ToppleBitModding", ensure_exists=True
        )

    if not options.no_window:
        root: tk.Tk = tk.Tk()
        root.title("ToppleBit Mod Installer")
        root.geometry("600x400")

        # Create frames for organization
        canvas = tk.Canvas(root, highlightthickness=0)
        canvas.pack(side=tk.LEFT, fill=tk.BOTH, expand=True)
        scrollbar = tk.Scrollbar(root, orient=tk.VERTICAL, command=canvas.yview)
        scrollbar.pack(side=tk.RIGHT, fill=tk.Y)
        canvas.config(yscrollcommand=scrollbar.set)

        frame = tk.Frame(canvas, padx=10, pady=10)
        canvas_window = canvas.create_window((0, 0), window=frame, anchor=tk.NW)

        def on_frame_configure(_=None):
            canvas.configure(scrollregion=canvas.bbox("all"))
            canvas.itemconfig(canvas_window, width=canvas.winfo_width())

        frame.bind("<Configure>", on_frame_configure)
        canvas.bind("<Configure>", on_frame_configure)

        # Enable mouse wheel scrolling
        def _on_mousewheel(event: Any):
            if sys.platform == "darwin":
                canvas.yview_scroll(-1 * int(event.delta), "units")
            else:
                canvas.yview_scroll(-1 * int(event.delta / 120), "units")

        canvas.bind_all("<MouseWheel>", _on_mousewheel)
        canvas.bind_all("<Button-4>", lambda event: canvas.yview_scroll(-1, "units"))
        canvas.bind_all("<Button-5>", lambda event: canvas.yview_scroll(1, "units"))

        # Installer install path
        tk.Label(frame, text="Installer Install Path:*").pack(anchor=tk.W)
        installer_path_frame = tk.Frame(frame)
        installer_path_frame.pack(anchor=tk.W, pady=(0, 10), expand=True, fill=tk.X)
        installer_path_entry = tk.Entry(installer_path_frame)
        installer_path_entry.pack(side=tk.LEFT, expand=True, fill=tk.X)
        if options.installer_install_path:
            installer_path_entry.insert(0, options.installer_install_path)
        installer_path_button = tk.Button(
            installer_path_frame,
            text="Browse files",
            command=lambda: (
                installer_path_entry.delete(0, tk.END)
                or installer_path_entry.insert(0, filedialog.askdirectory())
            ),
        )
        installer_path_button.pack(side=tk.LEFT, padx=(5, 0))

        # Settings path
        tk.Label(frame, text="Settings Path:*").pack(anchor=tk.W)
        settings_path_frame = tk.Frame(frame)
        settings_path_frame.pack(anchor=tk.W, pady=(0, 10), expand=True, fill=tk.X)
        settings_path_entry = tk.Entry(settings_path_frame)
        settings_path_entry.pack(side=tk.LEFT, expand=True, fill=tk.X)
        if options.setting_save_path:
            settings_path_entry.insert(0, options.setting_save_path)
        settings_path_button = tk.Button(
            settings_path_frame,
            text="Browse files",
            command=lambda: (
                settings_path_entry.delete(0, tk.END)
                or settings_path_entry.insert(
                    0,
                    filedialog.asksaveasfilename(
                        defaultextension=".yaml",
                        filetypes=[("YAML files", "*.yaml"), ("All files", "*.*")],
                    ),
                )
            ),
        )
        settings_path_button.pack(side=tk.LEFT, padx=(5, 0))

        # Game install path
        tk.Label(frame, text="Game Install Path:*").pack(anchor=tk.W)
        game_path_frame = tk.Frame(frame)
        game_path_frame.pack(anchor=tk.W, pady=(0, 10), expand=True, fill=tk.X)
        game_path_entry = tk.Entry(game_path_frame)
        game_path_entry.pack(side=tk.LEFT, expand=True, fill=tk.X)
        if options.game_install_path:
            game_path_entry.insert(0, options.game_install_path)
        game_path_button = tk.Button(
            game_path_frame,
            text="Browse files",
            command=lambda: (
                game_path_entry.delete(0, tk.END)
                or game_path_entry.insert(
                    0,
                    filedialog.askopenfilename(
                        filetypes=[("Executable files", "*.exe"), ("All files", "*.*")],
                    ),
                )
            ),
        )
        game_path_button.pack(side=tk.LEFT, padx=(5, 0))

        # Mod list
        mod_list_frame = tk.Frame(frame)
        mod_list_frame.pack(anchor=tk.W, pady=(0, 10), expand=True, fill=tk.X)
        tk.Label(mod_list_frame, text="Mods to Install (comma-separated):").pack(
            anchor=tk.W
        )
        mod_list_scrollbar = tk.Scrollbar(mod_list_frame, orient=tk.VERTICAL)
        mod_list_box = tk.Listbox(
            mod_list_frame,
            selectmode=tk.MULTIPLE,
            yscrollcommand=mod_list_scrollbar.set,
            height=min(5, len(AVAILABLE_MODS)),
        )
        mod_list_scrollbar.config(command=mod_list_box.yview)
        mod_list_scrollbar.pack(side=tk.RIGHT, fill=tk.Y)
        mod_list_box.pack(anchor=tk.W, pady=(0, 10), fill=tk.X)
        for mod in AVAILABLE_MODS.keys():
            mod_list_box.insert(tk.END, mod)
            if mod in options.mod_list:
                mod_list_box.selection_set(tk.END)

        # Checkboxes
        backup_var = tk.BooleanVar(value=options.backup_before_install)
        tk.Checkbutton(
            frame, text="Backup before installing mods", variable=backup_var
        ).pack(anchor=tk.W)

        restore_backup_var = tk.BooleanVar(value=options.restore_backup_on_failure)
        tk.Checkbutton(
            frame, text="Restore backup on failure", variable=restore_backup_var
        ).pack(anchor=tk.W)

        start_on_startup_var = tk.BooleanVar(value=options.start_on_startup)
        tk.Checkbutton(
            frame,
            text="Start launcher on system startup",
            variable=start_on_startup_var,
        ).pack(anchor=tk.W)

        auto_run_var = tk.BooleanVar(value=options.auto_run)
        tk.Checkbutton(
            frame, text="Auto-run game after install", variable=auto_run_var
        ).pack(anchor=tk.W)

        auto_update_installer_var = tk.BooleanVar(value=options.auto_update_installer)
        tk.Checkbutton(
            frame, text="Auto-update installer", variable=auto_update_installer_var
        ).pack(anchor=tk.W)

        auto_update_game_var = tk.BooleanVar(value=options.auto_update_game)
        tk.Checkbutton(
            frame, text="Auto-update game", variable=auto_update_game_var
        ).pack(anchor=tk.W)

        auto_update_var = tk.BooleanVar(value=options.auto_update_mods)
        tk.Checkbutton(frame, text="Auto-update mods", variable=auto_update_var).pack(
            anchor=tk.W
        )

        submitted = False

        def on_submit():
            if not installer_path_entry.get().strip():
                messagebox.showerror("Error", "Installer install path is required.")
                return
            if not settings_path_entry.get().strip():
                messagebox.showerror("Error", "Settings path is required.")
                return
            if not game_path_entry.get().strip():
                messagebox.showerror("Error", "Game install path is required.")
                return
            if not game_path_entry.get().strip().endswith("ToppleBit.exe"):
                messagebox.showerror(
                    "Error",
                    "Please select the correct ToppleBit.exe file for the game install path.",
                )
                return

            options.installer_install_path = installer_path_entry.get()
            options.setting_save_path = settings_path_entry.get()
            options.game_install_path = game_path_entry.get()
            options.mod_list = [
                mod_list_box.get(i)
                for i in cast(Callable[[], list[int]], mod_list_box.curselection)()
            ]
            options.backup_before_install = backup_var.get()
            options.restore_backup_on_failure = restore_backup_var.get()
            options.start_on_startup = start_on_startup_var.get()
            options.auto_run = auto_run_var.get()
            options.auto_update_installer = auto_update_installer_var.get()
            options.auto_update_game = auto_update_game_var.get()
            options.auto_update_mods = auto_update_var.get()
            nonlocal submitted
            submitted = True
            root.destroy()

        tk.Button(frame, text="Continue", command=on_submit).pack(pady=20)

        # Wait for user to complete the setup in GUI before proceeding
        root.mainloop()

        if not submitted:
            logging.info("Setup cancelled by user.")
            sys.exit(0)

    if not options.game_install_path or not options.game_install_path.strip().endswith(
        "ToppleBit.exe"
    ):
        messagebox.showerror(
            "Error",
            "Please input the correct ToppleBit.exe file for the game install path.",
        )
        sys.exit(1)

    os.makedirs(options.installer_install_path, exist_ok=True)

    if not sys.argv[0].endswith(".py"):
        shutil.copyfile(
            sys.argv[0],
            os.path.join(options.installer_install_path, os.path.basename(sys.argv[0])),
        )
    else:
        logging.info("Running from .py file; downloading installer.")
        response = requests.get(INSTALLER_DOWNLOAD_URL)
        response.raise_for_status()
        with open(
            os.path.join(
                options.installer_install_path, "ToppleBitModdingLauncher.exe"
            ),
            "wb",
        ) as f:
            f.write(response.content)

    with open(
        os.path.join(options.installer_install_path, "config"),
        "w",
    ) as f:
        f.write(os.path.abspath(options.setting_save_path))

    options.game_install_path = os.path.dirname(os.path.abspath(options.game_install_path))
    save_options(options)

    launcher(options)


if __name__ == "__main__":
    main()

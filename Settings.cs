using DSharpPlus.Entities;
using DSharpPlus.Net.Models;
using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace Aeternum.Properties {
    
    
    // Tato třída umožňuje zpracovávat specifické události v třídě nastavení:
    //  Událost SettingChanging se vyvolá před změnou hodnoty nastavení.
    //  Událost PropertyChanged se vyvolá po změně hodnoty nastavení.
    //  Událost SettingsLoaded se vyvolá po načtení hodnot nastavení.
    //  Událost SettingsSaving se vyvolá před uložením hodnot nastavení.
    public sealed partial class Settings {
        
        public Settings() {
            // // Pro přidávání obslužných rutin událostí určených pro ukládání a změnu nastavení odkomentujte prosím níže uvedené řádky:
            //
            // this.SettingChanging += this.SettingChangingEventHandler;
            //
            // this.SettingsSaving += this.SettingsSavingEventHandler;
            //
        }
    }
}

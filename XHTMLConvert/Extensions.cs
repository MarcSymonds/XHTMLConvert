using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XHTMLConvert {
   public static class Extensions {
      static Extensions() {  }

      public static bool In(this string value, string[] values, bool ignoreCase) {
         if (value != null && values != null) {
            foreach(string v in values) {
               if (value.Equals(v, (ignoreCase ? StringComparison.CurrentCultureIgnoreCase : StringComparison.CurrentCulture))) {
                  return true;
               }
            }
         }

         return false;
      }
   }

}

import {
  MAT_ERROR,
  MAT_FORM_FIELD,
  MAT_FORM_FIELD_DEFAULT_OPTIONS,
  MAT_PREFIX,
  MAT_SUFFIX,
  MatError,
  MatFormField,
  MatFormFieldControl,
  MatFormFieldModule,
  MatHint,
  MatLabel,
  MatPrefix,
  MatSuffix,
  getMatFormFieldDuplicatedHintError,
  getMatFormFieldMissingControlError,
  getMatFormFieldPlaceholderConflictError
} from "./chunk-QOIKD2WR.js";
import "./chunk-6SHRLCKI.js";
import "./chunk-KXC4HKWR.js";
import "./chunk-FD5AMYSS.js";
import "./chunk-YIW5HYHF.js";
import "./chunk-L5M6JFEJ.js";
import "./chunk-WT6CVABJ.js";
import "./chunk-Q3IJFJIL.js";
import "./chunk-GV5LUSDY.js";
import "./chunk-DG6N4IH3.js";
import "./chunk-42FJBLFI.js";
import "./chunk-2O4WY5GE.js";
import "./chunk-ZUECRCB4.js";
import "./chunk-TUOOLWPT.js";
import "./chunk-U6MUGMWF.js";
import "./chunk-XUATCLPK.js";
import "./chunk-4BMJA52G.js";
import "./chunk-KC6ELNRI.js";
import "./chunk-QBLHZMMV.js";
import "./chunk-ZTOR4A6Q.js";
import "./chunk-3Z4NRYXA.js";
import "./chunk-TGJDCTWR.js";
import "./chunk-BKZ2WJQX.js";
import "./chunk-3OV72XIM.js";

// node_modules/@angular/material/fesm2022/form-field.mjs
var matFormFieldAnimations = {
  // Represents:
  // trigger('transitionMessages', [
  //   // TODO(mmalerba): Use angular animations for label animation as well.
  //   state('enter', style({opacity: 1, transform: 'translateY(0%)'})),
  //   transition('void => enter', [
  //     style({opacity: 0, transform: 'translateY(-5px)'}),
  //     animate('300ms cubic-bezier(0.55, 0, 0.55, 0.2)'),
  //   ]),
  // ])
  /** Animation that transitions the form field's error and hint messages. */
  transitionMessages: {
    type: 7,
    name: "transitionMessages",
    definitions: [{
      type: 0,
      name: "enter",
      styles: {
        type: 6,
        styles: {
          opacity: 1,
          transform: "translateY(0%)"
        },
        offset: null
      }
    }, {
      type: 1,
      expr: "void => enter",
      animation: [{
        type: 6,
        styles: {
          opacity: 0,
          transform: "translateY(-5px)"
        },
        offset: null
      }, {
        type: 4,
        styles: null,
        timings: "300ms cubic-bezier(0.55, 0, 0.55, 0.2)"
      }],
      options: null
    }],
    options: {}
  }
};
export {
  MAT_ERROR,
  MAT_FORM_FIELD,
  MAT_FORM_FIELD_DEFAULT_OPTIONS,
  MAT_PREFIX,
  MAT_SUFFIX,
  MatError,
  MatFormField,
  MatFormFieldControl,
  MatFormFieldModule,
  MatHint,
  MatLabel,
  MatPrefix,
  MatSuffix,
  getMatFormFieldDuplicatedHintError,
  getMatFormFieldMissingControlError,
  getMatFormFieldPlaceholderConflictError,
  matFormFieldAnimations
};
//# sourceMappingURL=@angular_material_form-field.js.map

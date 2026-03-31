import {
  VERSION
} from "./chunk-ZTOR4A6Q.js";

// node_modules/@angular/cdk/fesm2022/backwards-compatibility-DHR38MsD.mjs
function _bindEventWithOptions(renderer, target, eventName, callback, options) {
  const major = parseInt(VERSION.major);
  const minor = parseInt(VERSION.minor);
  if (major > 19 || major === 19 && minor > 0 || major === 0 && minor === 0) {
    return renderer.listen(target, eventName, callback, options);
  }
  target.addEventListener(eventName, callback, options);
  return () => {
    target.removeEventListener(eventName, callback, options);
  };
}

export {
  _bindEventWithOptions
};
//# sourceMappingURL=chunk-XUATCLPK.js.map

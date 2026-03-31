import axios from "axios";

export const http = axios.create({
  baseURL: import.meta.env.NG_APP_API_URL,
});

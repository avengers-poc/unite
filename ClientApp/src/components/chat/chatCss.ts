import { ThemeUIStyleObject } from "@infotrack/zenith-ui";

export const chatContent: ThemeUIStyleObject = {
  width: "100%",
  minWidth: "600px",
  height: "100%",
  flexDirection: "column",
  boxSizing: "border-box",
  padding: "0 10px",
};

export const messageContent: ThemeUIStyleObject = {
  minHeight: `calc(100vh - 100px)`,
  paddingBottom: `7rem`,
};

export const inputMessage = (disable: boolean): ThemeUIStyleObject => ({
  position: "fixed",
  bottom: 0,
  left: 0,
  width: "100%",
  padding: "1rem",
  backgroundColor: "#b3bdc7",
  textAlign: "center",

  "> .form": {
    display: "flex",
    width: "100%",
    backgroundColor: disable ? "#f0f3f6" : "#fff",
    boxShadow: "0px 1px 4px 0px rgba(0,0,0,0.1)",
    borderRadius: "10px",
  },

  "> .form .input": {
    fontSize: "16px",
    border: 0,
    outline: "none",
    flexGrow: 1,
    padding: "20px",
    borderRadius: "10px",
  },

  "> .form .button": {
    padding: "12px 14px",
    fontSize: "16px",
    backgroundColor: disable ? "#dbe0e6" : "#f2632e",
    color: "#fff",
    border: "none",
    borderRadius: "8px",
    margin: "14px",
    cursor: "pointer",
  },
});

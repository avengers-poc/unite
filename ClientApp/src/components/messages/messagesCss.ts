import { ThemeUIStyleObject } from "@infotrack/zenith-ui";

export const messagesList: ThemeUIStyleObject = {
  listStyle: "none",
  paddingLeft: 0,
  flexGrow: 1,
  overflow: "auto",
};

export const messageItem = (isBot: boolean): ThemeUIStyleObject => ({
  display: "flex",
  marginTop: "10px",
  flexDirection: isBot ? "row" : "row-reverse",
  textAlign: isBot ? "left" : "right",
  "> .avatar": {
    margin: "0 10px -10px",
  },
});

export const messageContent: ThemeUIStyleObject = {
  maxWidth: "95%",
};

export const avatar = (isBot: boolean): ThemeUIStyleObject => ({
  display: "inline-block",
  height: "35px",
  width: "35px",
  minWidth: "35px",
  borderRadius: "50%",
  backgroundColor: isBot ? "#f8fafb" : "#f2632e",
});

export const username: ThemeUIStyleObject = {
  color: "#000",
  fontSize: "14px",
  paddingBottom: "4px",
};

export const text: ThemeUIStyleObject = {
  padding: "10px",
  width: "100%",
  margin: "0",
  borderRadius: "12px",
  backgroundColor: "#f8fafb",
  color: "#000",
  display: "inline-block",
};

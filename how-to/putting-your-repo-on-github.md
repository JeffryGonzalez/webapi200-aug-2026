# Putting your repo on GitHub

If you've already put your repo on GitHub, you obviously can skip this.

We are going to use the GitHub CLI (Command Line Interface) for this. There are lots of ways to accomplish this, but I like using the GitHub CLI (`gh`)

**Warning: You *must* be logged into your GitHub account with your default browser for this to work.**

## Configure your git settings

In the **HOME BASE** VSCode (the one that is c:/users/student/class), open an integrated terminal. (Ctrl+~)

Enter the following, replacing the values with your name and email address. You can use the name and email address you use for your github account (nothing checks it, it's just added to your commit messages)

```sh
git config --global user.name "Your Name"
git config --global user.email "your@email.com"
```

## Initialize a git repo and commit

In that same terminal window:

```sh
git init
git add .
git commit -m "Initial"
```

## Connect your local git client with GitHub

In that same terminal window:

```sh
gh auth login
```

This will step you through several questions. Use your keyboard arrow keys to make selections.

- Where do you use GitHub? (GitHub.com)
- What is your preferred protocol for Git operations on this host? (HTTPS)
-  Authenticate Git with your GitHub credentials? (Y/n) (hit enter, the default is "Y")
- How would you like to authenticate GitHub CLI? (Login with a web browser)

It will now give you a one-time code. Copy that code (select it with your mouse, then Ctrl+C - be careful to only hit Ctrl+C once, or it will kill the process and you'll have to start over!)

After you have copied the one-time code, hit enter in the terminal. Your web browser will open, ask you to confirm who you are, and then request that code.

Finally, it will confirm that you want to authorize the CLI. Make your decision to authorize.

> Note: depending on how your security is configured on your GitHub account, you may have to provide a form of two-factor-authentication.

You can return to the terminal in VS Code and it should indicate you have been authenticated.

## Push your repo to github

In this step you will use the GitHub CLI to create a repository on GitHub, tell your local git client that it is the "origin", and push your work to it.

In that same terminal in home-base VS Code:

```
gh repo create
```

Questions and Answers:

- What would you like to do? (Push an existing local repository to github.com) - use your arrow keys to go to the last selection and hit enter.
- Path to repository (.) (Hit enter - the period there is the default - this directory)
- Repository name (default is class, name it something you'll recognize later)
- If your github account is associated with multiple organizations, you may be asked to select an organization at this step. Make your choice, hit enter.
- Description (optional, but appears on your GitHub repo list.)
- Visibility (Choose either public or private. Public means anyone on GitHub can see it)
- For the rest of the questions you can take the default
    - Add a remote (Y/n) - (Yes. Adds configuration in this .git directory to know about your GitHub repository)
    - What should the remote be called? (Origin - the default is good.)
    - Would you like to push commits from the current branch to 'origin'? (Yes)


You are done! 

## View your repo on GitHub

In the terminal, type:

```sh
gh browse
```
This will open your browser with the location of your GitHub repo.

## Keeping your code updated

You can use the terminal or the UI in VS Code to make frequent commits, and push the changes to GitHub.

If you need a refresher, see [Basics of Source Control](https://class.hypertheory-labs.com/reference/angular-prework/the-development-environment/the-basics-of-source-code-control/). It lives under the Angular prework because that is where it was written, and it is
there for anyone taking any course — nothing in it is Angular-specific.

## This stuff can be a pain if you are new to Git

If you get at all frustrated, just ask me (Jeff) to help during a break or something. No biggie.

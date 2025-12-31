# Setting up a Local GitHub Actions Runner

Since this project relies on local Streamer.bot DLLs that are not in the git repository, we use a **self-hosted runner** to build the project.

## 1. Create a Runner in GitHub
1. Go to your repository on GitHub: [rapidbnnuy/PNGTuber-GPTv2](https://github.com/rapidbnnuy/PNGTuber-GPTv2)
2. Navigate to **Settings** > **Actions** > **Runners**.
3. Click **New self-hosted runner**.
4. Select **macOS**.
5. Follow the commands provided by GitHub to Download and Configure the runner. 
   - *Recommendation*: Create a folder named `actions-runner` in your `Developer` folder or home directory.

## 2. Configure Dependencies
The project expects `Streamer.bot-x64-1.0.1/Streamer.bot.Plugin.Interface.dll` to exist at the repository root level. When the runner checks out the code, it enters a `_work` directory.

1. Start the runner once to generate the folder structure (or manually create it).
   - It will look something like: `.../actions-runner/_work/PNGTuber-GPTv2/PNGTuber-GPTv2/`
2. **Copy the Streamer.bot directory** to the runner's working directory.
   - Source: `/Users/cbruscato/Developer/PNGTuber-GPTv2/Streamer.bot-x64-1.0.1`
   - Destination: `.../actions-runner/_work/PNGTuber-GPTv2/Streamer.bot-x64-1.0.1`

   **Example Command:**
   ```bash
   # Assuming you are in the actions-runner folder
   cp -r /Users/cbruscato/Developer/PNGTuber-GPTv2/Streamer.bot-x64-1.0.1 _work/PNGTuber-GPTv2/
   ```

   *Note: You may need to do this again if you clean the runner workspace.*

## 3. Run the Runner
Execute the run script to start listening for jobs:
```bash
./run.sh
```

Now, when you push to `main`, the workflow defined in `.github/workflows/build.yml` will pick up this runner and execute the build.

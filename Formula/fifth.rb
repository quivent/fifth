# Homebrew formula for Fifth
# Usage: brew tap quivent/fifth && brew install fifth

class Fifth < Formula
  desc "A Forth for the agentic era - self-contained interpreter with zero dependencies"
  homepage "https://github.com/quivent/fifth"
  version "0.1.0"
  license "MIT"

  on_macos do
    if Hardware::CPU.arm?
      url "https://github.com/quivent/fifth/releases/download/v0.1.0/fifth-darwin-arm64.tar.gz"
      sha256 "REPLACE_WITH_SHA256_ARM64"
    else
      url "https://github.com/quivent/fifth/releases/download/v0.1.0/fifth-darwin-x86_64.tar.gz"
      sha256 "REPLACE_WITH_SHA256_X86_64"
    end
  end

  on_linux do
    url "https://github.com/quivent/fifth/releases/download/v0.1.0/fifth-linux-x86_64.tar.gz"
    sha256 "REPLACE_WITH_SHA256_LINUX"
  end

  def install
    bin.install "fifth"

    # Install libraries to share
    (share/"fifth/lib").install Dir["lib/*"] if Dir.exist?("lib")
  end

  def post_install
    # Set up user's ~/.fifth directory
    fifth_home = ENV["FIFTH_HOME"] || "#{ENV["HOME"]}/.fifth"
    system "mkdir", "-p", "#{fifth_home}/lib", "#{fifth_home}/packages"

    # Copy libraries if not present
    Dir["#{share}/fifth/lib/*"].each do |f|
      target = "#{fifth_home}/lib/#{File.basename(f)}"
      system "cp", "-n", f, target unless File.exist?(target)
    end
  end

  test do
    assert_equal "5", shell_output("#{bin}/fifth -e '2 3 + . cr'").strip
  end
end
